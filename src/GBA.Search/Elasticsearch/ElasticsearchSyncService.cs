using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Configuration;
using GBA.Search.Sync;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Elasticsearch;

public interface IElasticsearchSyncService {
    Task<SyncResult> FullRebuildAsync(CancellationToken ct = default);
    Task<SyncResult> IncrementalSyncAsync(CancellationToken ct = default);
}

public sealed class ElasticsearchSyncService : IElasticsearchSyncService {
    private readonly HttpClient _http;
    private readonly ElasticsearchSettings _settings;
    private readonly SyncSettings _syncSettings;
    private readonly ProductSyncRepository _repository;
    private readonly IElasticsearchIndexService _indexService;
    private readonly ILogger<ElasticsearchSyncService> _log;

    private DateTime _lastSyncTime = DateTime.MinValue;
    private readonly Lock _syncLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ElasticsearchSyncService(
        HttpClient httpClient,
        IOptions<ElasticsearchSettings> settings,
        IOptions<SyncSettings> syncSettings,
        ProductSyncRepository repository,
        IElasticsearchIndexService indexService,
        ILogger<ElasticsearchSyncService> logger) {
        _http = httpClient;
        _settings = settings.Value;
        _syncSettings = syncSettings.Value;
        _repository = repository;
        _indexService = indexService;
        _log = logger;
    }

    public async Task<SyncResult> FullRebuildAsync(CancellationToken ct = default) {
        var sw = Stopwatch.StartNew();

        try {
            _log.LogInformation("Starting Elasticsearch full rebuild");

            // Delete and recreate index
            if (await _indexService.IndexExistsAsync(ct)) {
                await _indexService.DeleteIndexAsync(ct);
            }

            if (!await _indexService.CreateIndexAsync(ct)) {
                return SyncResult.Failed("Failed to create index");
            }

            // Fetch all products
            var products = await _repository.GetAllProductsAsync();
            _log.LogInformation("Fetched {Count} products from SQL", products.Count);

            var productIds = products.Select(p => p.Id).ToList();
            var originalNumbers = await _repository.GetOriginalNumbersForProductsAsync(productIds);
            _log.LogInformation("Fetched original numbers for {Count} products", originalNumbers.Count);

            // Index in batches
            int totalIndexed = 0;
            var batch = new List<ProductDocument>(_syncSettings.BatchSize);

            foreach (var product in products) {
                var doc = CreateDocument(product, originalNumbers.GetValueOrDefault(product.Id));
                batch.Add(doc);

                if (batch.Count >= _syncSettings.BatchSize) {
                    var indexed = await BulkIndexAsync(batch, ct);
                    totalIndexed += indexed;
                    batch.Clear();
                    _log.LogDebug("Indexed batch, total: {Total}", totalIndexed);
                }
            }

            if (batch.Count > 0) {
                var indexed = await BulkIndexAsync(batch, ct);
                totalIndexed += indexed;
            }

            // Refresh index
            await _http.PostAsync($"{_settings.IndexName}/_refresh", null, ct);

            sw.Stop();

            lock (_syncLock) {
                _lastSyncTime = DateTime.UtcNow;
            }

            _log.LogInformation(
                "Elasticsearch full rebuild completed: {Total} documents indexed in {ElapsedMs}ms",
                totalIndexed, sw.ElapsedMilliseconds);

            return new SyncResult {
                Success = true,
                DocumentsIndexed = totalIndexed,
                DocumentsDeleted = 0,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        } catch (Exception ex) {
            _log.LogError(ex, "Elasticsearch full rebuild failed");
            return SyncResult.Failed(ex.Message);
        }
    }

    public async Task<SyncResult> IncrementalSyncAsync(CancellationToken ct = default) {
        var sw = Stopwatch.StartNew();

        DateTime syncSince;
        lock (_syncLock) {
            syncSince = _lastSyncTime;
        }

        if (syncSince == DateTime.MinValue) {
            _log.LogInformation("First sync - performing full rebuild");
            return await FullRebuildAsync(ct);
        }

        try {
            var changedProducts = await _repository.GetChangedProductsAsync(syncSince);
            var deletedIds = await _repository.GetDeletedProductIdsAsync(syncSince);

            if (changedProducts.Count == 0 && deletedIds.Count == 0) {
                lock (_syncLock) {
                    _lastSyncTime = DateTime.UtcNow;
                }
                return new SyncResult {
                    Success = true,
                    DocumentsIndexed = 0,
                    DocumentsDeleted = 0,
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            var productIds = changedProducts.Select(p => p.Id).ToList();
            var originalNumbers = await _repository.GetOriginalNumbersForProductsAsync(productIds);

            var documents = changedProducts
                .Select(p => CreateDocument(p, originalNumbers.GetValueOrDefault(p.Id)))
                .ToList();

            int indexed = await BulkIndexAsync(documents, ct);
            int deleted = await BulkDeleteAsync(deletedIds, ct);

            await _http.PostAsync($"{_settings.IndexName}/_refresh", null, ct);

            sw.Stop();

            lock (_syncLock) {
                _lastSyncTime = DateTime.UtcNow;
            }

            _log.LogInformation(
                "Elasticsearch incremental sync: {Indexed} indexed, {Deleted} deleted in {ElapsedMs}ms",
                indexed, deleted, sw.ElapsedMilliseconds);

            return new SyncResult {
                Success = true,
                DocumentsIndexed = indexed,
                DocumentsDeleted = deleted,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        } catch (Exception ex) {
            _log.LogError(ex, "Elasticsearch incremental sync failed");
            return SyncResult.Failed(ex.Message);
        }
    }

    private async Task<int> BulkIndexAsync(List<ProductDocument> documents, CancellationToken ct) {
        if (documents.Count == 0) return 0;

        var sb = new StringBuilder();
        foreach (var doc in documents) {
            sb.AppendLine(JsonSerializer.Serialize(new { index = new { _index = _settings.IndexName, _id = doc.Id } }, JsonOptions));
            sb.AppendLine(JsonSerializer.Serialize(doc, JsonOptions));
        }

        var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-ndjson");
        var response = await _http.PostAsync("_bulk", content, ct);

        if (!response.IsSuccessStatusCode) {
            var error = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("Bulk index failed with HTTP error: {Error}", error);
            return 0;
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;

        if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.GetBoolean()) {
            int successCount = 0;
            int errorCount = 0;
            if (root.TryGetProperty("items", out var items)) {
                foreach (var item in items.EnumerateArray()) {
                    if (item.TryGetProperty("index", out var indexResult)) {
                        if (indexResult.TryGetProperty("status", out var status) && status.GetInt32() < 300) {
                            successCount++;
                        } else {
                            errorCount++;
                            if (errorCount <= 3 && indexResult.TryGetProperty("error", out var errorInfo)) {
                                _log.LogWarning("Bulk index document error: {Error}", errorInfo.ToString());
                            }
                        }
                    }
                }
            }
            if (errorCount > 3) {
                _log.LogWarning("Bulk index had {ErrorCount} additional errors (not logged)", errorCount - 3);
            }
            return successCount;
        }

        return documents.Count;
    }

    private async Task<int> BulkDeleteAsync(List<long> ids, CancellationToken ct) {
        if (ids.Count == 0) return 0;

        var sb = new StringBuilder();
        foreach (var id in ids) {
            sb.AppendLine(JsonSerializer.Serialize(new { delete = new { _index = _settings.IndexName, _id = id } }, JsonOptions));
        }

        var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-ndjson");
        var response = await _http.PostAsync("_bulk", content, ct);

        if (!response.IsSuccessStatusCode) {
            var error = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("Bulk delete failed: {Error}", error);
            return 0;
        }

        return ids.Count;
    }

    private static ProductDocument CreateDocument(ProductSyncData data, List<string>? origNumbers) {
        origNumbers ??= [];

        return new ProductDocument {
            Id = data.Id,
            NetUid = data.NetUid.ToString(),
            VendorCode = data.VendorCode ?? "",
            VendorCodeClean = NumberNormalizer.Normalize(data.SearchVendorCode),
            Name = data.Name ?? "",
            NameUA = data.NameUA ?? "",
            Description = data.Description ?? "",
            DescriptionUA = data.DescriptionUA ?? "",
            SearchName = data.SearchName ?? "",
            SearchNameUA = data.SearchNameUA ?? "",
            SearchDescription = data.SearchDescription ?? "",
            SearchDescriptionUA = data.SearchDescriptionUA ?? "",
            MainOriginalNumber = data.MainOriginalNumber ?? "",
            MainOriginalNumberClean = NumberNormalizer.Normalize(data.MainOriginalNumber),
            OriginalNumbers = origNumbers,
            OriginalNumbersClean = origNumbers.Select(NumberNormalizer.Normalize).Distinct().ToList(),
            Size = data.Size ?? "",
            SizeClean = NumberNormalizer.Normalize(data.Size),
            PackingStandard = data.PackingStandard ?? "",
            OrderStandard = data.OrderStandard ?? "",
            Ucgfea = data.Ucgfea ?? "",
            Volume = data.Volume ?? "",
            Top = data.Top ?? "",
            Weight = data.Weight,
            HasAnalogue = data.HasAnalogue,
            HasComponent = data.HasComponent,
            HasImage = data.HasImage,
            Image = data.Image ?? "",
            MeasureUnitId = data.MeasureUnitId,
            Available = data.AvailableQty > 0,
            AvailableQtyUk = data.AvailableQtyUk + data.AvailableQtyUkVat,
            AvailableQtyUkVat = data.AvailableQtyUkVat,
            AvailableQtyPl = data.AvailableQtyPl + data.AvailableQtyPlVat,
            AvailableQtyPlVat = data.AvailableQtyPlVat,
            AvailableQty = data.AvailableQty,
            IsForWeb = data.IsForWeb,
            IsForSale = data.IsForSale,
            IsForZeroSale = data.IsForZeroSale,
            SlugId = data.SlugId,
            SlugNetUid = data.SlugNetUid.ToString(),
            SlugUrl = data.SlugUrl ?? "",
            SlugLocale = data.SlugLocale ?? "",
            RetailPrice = data.RetailPrice,
            RetailPriceVat = data.RetailPriceVat,
            RetailCurrencyCode = data.RetailCurrencyCode ?? "UAH",
            UpdatedAt = data.Updated
        };
    }
}
