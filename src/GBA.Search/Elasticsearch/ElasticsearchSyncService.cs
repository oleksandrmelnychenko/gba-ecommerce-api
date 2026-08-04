using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GBA.Common.Search;
using GBA.Search.Configuration;
using GBA.Search.Sync;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Elasticsearch;

public interface IElasticsearchSyncService {
    Task<SyncResult> FullRebuildAsync(CancellationToken ct = default);
    Task<SyncResult> IncrementalSyncAsync(CancellationToken ct = default);

    /// <summary>Re-indexes a specific set of products immediately (targeted, near-real-time).</summary>
    Task<SyncResult> ReindexProductsAsync(IReadOnlyCollection<long> productIds, CancellationToken ct = default);
}

public sealed class ElasticsearchSyncService : IElasticsearchSyncService {
    private readonly HttpClient _http;
    private readonly ElasticsearchSettings _settings;
    private readonly SyncSettings _syncSettings;
    private readonly ProductSyncRepository _repository;
    private readonly IElasticsearchIndexService _indexService;
    private readonly ISearchSyncStateStore _state;
    private readonly ISearchCacheInvalidator _cacheInvalidator;
    private readonly ILogger<ElasticsearchSyncService> _log;

    // Re-scan a small window before the last watermark so rows written during the previous
    // run are never missed (bulk upserts are idempotent, so overlap is harmless).
    private const int WatermarkOverlapSeconds = 120;

    // Process-wide single-flight: never let a rebuild and an incremental run overlap.
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static IEnumerable<T[]> PartitionBulkItems<T>(
        IReadOnlyCollection<T> items,
        int batchSize) {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        return items.Chunk(batchSize);
    }

    internal static void EnsureSuccessfulHttpResponse(
        HttpResponseMessage response,
        string operation,
        string responseBody) {
        if (response.IsSuccessStatusCode) return;

        string details = string.IsNullOrWhiteSpace(responseBody)
            ? response.ReasonPhrase ?? "empty response"
            : responseBody;
        throw new HttpRequestException(
            $"Elasticsearch {operation} failed with HTTP {(int)response.StatusCode} "
            + $"({response.StatusCode}): {details}",
            null,
            response.StatusCode);
    }

    internal static int ValidateBulkResponse(
        string responseBody,
        string actionName,
        int expectedCount,
        bool allowNotFound = false) {
        using JsonDocument jsonDoc = JsonDocument.Parse(responseBody);
        JsonElement root = jsonDoc.RootElement;

        if (!root.TryGetProperty("errors", out JsonElement errorsElement)
            || errorsElement.ValueKind is not JsonValueKind.True and not JsonValueKind.False) {
            throw new InvalidOperationException(
                $"Elasticsearch bulk {actionName} returned an invalid response without an errors flag.");
        }

        if (!errorsElement.GetBoolean()) return expectedCount;

        if (!root.TryGetProperty("items", out JsonElement items)
            || items.ValueKind != JsonValueKind.Array) {
            throw new InvalidOperationException(
                $"Elasticsearch bulk {actionName} reported errors without item details.");
        }

        int errorCount = 0;
        string? firstError = null;
        foreach (JsonElement item in items.EnumerateArray()) {
            if (!item.TryGetProperty(actionName, out JsonElement result)) {
                errorCount++;
                firstError ??= "missing action result";
                continue;
            }

            int status = result.TryGetProperty("status", out JsonElement statusElement)
                ? statusElement.GetInt32()
                : 0;
            if (status is >= 200 and < 300 || allowNotFound && status == 404) continue;

            errorCount++;
            if (firstError == null) {
                firstError = result.TryGetProperty("error", out JsonElement error)
                    ? error.ToString()
                    : $"HTTP {status}";
            }
        }

        if (errorCount > 0) {
            throw new InvalidOperationException(
                $"Elasticsearch bulk {actionName} failed for {errorCount}/{expectedCount} items. "
                + $"First error: {firstError}");
        }

        return expectedCount;
    }

    public ElasticsearchSyncService(
        HttpClient httpClient,
        IOptions<ElasticsearchSettings> settings,
        IOptions<SyncSettings> syncSettings,
        ProductSyncRepository repository,
        IElasticsearchIndexService indexService,
        ISearchSyncStateStore state,
        ISearchCacheInvalidator cacheInvalidator,
        ILogger<ElasticsearchSyncService> logger) {
        _http = httpClient;
        _settings = settings.Value;
        _syncSettings = syncSettings.Value;
        _repository = repository;
        _indexService = indexService;
        _state = state;
        _cacheInvalidator = cacheInvalidator;
        _log = logger;
    }

    public async Task<SyncResult> FullRebuildAsync(CancellationToken ct = default) {
        DateTime runStart = DateTime.UtcNow;
        Stopwatch sw = Stopwatch.StartNew();

        await _gate.WaitAsync(ct);
        try {
            _log.LogInformation("Starting Elasticsearch full rebuild (alias-swap)");

            // Build into a brand-new index; the live alias keeps serving the current one
            // until we atomically swap at the end -> zero search downtime during rebuild.
            string? newIndex = await _indexService.CreateVersionedIndexAsync(ct);
            if (newIndex == null) {
                return SyncResult.Failed("Failed to create index");
            }

            // Fetch all products
            List<ProductSyncData> products = await _repository.GetAllProductsAsync();
            _log.LogInformation("Fetched {Count} products from SQL", products.Count);

            List<long> productIds = products.Select(p => p.Id).ToList();
            Dictionary<long, List<string>> originalNumbers = await _repository.GetOriginalNumbersForProductsAsync(productIds);
            _log.LogInformation("Fetched original numbers for {Count} products", originalNumbers.Count);

            // Index in batches into the new index
            int totalIndexed = 0;
            List<ProductDocument> batch = new List<ProductDocument>(_syncSettings.BatchSize);

            foreach (ProductSyncData product in products) {
                ProductDocument doc = CreateDocument(product, originalNumbers.GetValueOrDefault(product.Id));
                batch.Add(doc);

                if (batch.Count >= _syncSettings.BatchSize) {
                    totalIndexed += await BulkIndexAsync(batch, ct, newIndex);
                    batch.Clear();
                    _log.LogDebug("Indexed batch, total: {Total}", totalIndexed);
                }
            }

            if (batch.Count > 0) {
                totalIndexed += await BulkIndexAsync(batch, ct, newIndex);
            }

            // Re-enable normal near-real-time refreshes before exposing the new index.
            // Versioned rebuild indices are created with refresh_interval=-1 for fast bulk loading.
            StringContent refreshSettings = new(
                """{"index":{"refresh_interval":"1s"}}""",
                Encoding.UTF8,
                "application/json");
            HttpResponseMessage refreshSettingsResponse =
                await _http.PutAsync($"{newIndex}/_settings", refreshSettings, ct);
            string refreshSettingsBody = await refreshSettingsResponse.Content.ReadAsStringAsync(ct);
            EnsureSuccessfulHttpResponse(
                refreshSettingsResponse,
                "refresh interval restore",
                refreshSettingsBody);

            // Make the new index searchable, then atomically swap the alias and prune old indices.
            HttpResponseMessage refreshResponse = await _http.PostAsync($"{newIndex}/_refresh", null, ct);
            string refreshBody = await refreshResponse.Content.ReadAsStringAsync(ct);
            EnsureSuccessfulHttpResponse(refreshResponse, "index refresh", refreshBody);

            if (!await _indexService.SwapAliasAsync(newIndex, ct)) {
                return SyncResult.Failed("Failed to swap alias to new index");
            }

            if (_syncSettings.CleanupOldCollections) {
                await _indexService.CleanupOldVersionedIndicesAsync(_syncSettings.CollectionsToKeep, ct);
            }

            sw.Stop();

            await _state.SetWatermarkAsync(runStart, ct);
            await _cacheInvalidator.InvalidateProductsAsync(ct);

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
        } finally {
            _gate.Release();
        }
    }

    public async Task<SyncResult> IncrementalSyncAsync(CancellationToken ct = default) {
        DateTime runStart = DateTime.UtcNow;
        DateTime watermark = await _state.GetWatermarkAsync(ct);

        if (watermark == DateTime.MinValue) {
            _log.LogInformation("No sync watermark found - performing full rebuild");
            return await FullRebuildAsync(ct);
        }

        await _gate.WaitAsync(ct);
        Stopwatch sw = Stopwatch.StartNew();
        try {
            DateTime since = watermark.AddSeconds(-WatermarkOverlapSeconds);

            // Lightweight change detection (ids only), then reuse the fast by-ids reindex
            // path — avoids the heavy "all fields for all changed products" projection.
            List<long> changedIds = await _repository.GetChangedProductIdsAsync(since);
            List<long> deletedIds = await _repository.GetDeletedProductIdsAsync(since);

            if (changedIds.Count == 0 && deletedIds.Count == 0) {
                await _state.SetWatermarkAsync(runStart, ct);
                return new SyncResult { Success = true, ElapsedMs = sw.ElapsedMilliseconds };
            }

            HashSet<long> ids = new(changedIds);
            ids.UnionWith(deletedIds);

            (int indexed, int deleted) = await IndexByIdsAsync(ids, ct);

            sw.Stop();
            await _state.SetWatermarkAsync(runStart, ct);
            if (indexed > 0 || deleted > 0) {
                await _cacheInvalidator.InvalidateProductsAsync(ct);
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
        } finally {
            _gate.Release();
        }
    }

    public async Task<SyncResult> ReindexProductsAsync(IReadOnlyCollection<long> productIds, CancellationToken ct = default) {
        if (productIds.Count == 0) return new SyncResult { Success = true };

        Stopwatch sw = Stopwatch.StartNew();
        try {
            (int indexed, int deleted) = await IndexByIdsAsync(productIds, ct);
            sw.Stop();
            if (indexed > 0 || deleted > 0) {
                await _cacheInvalidator.InvalidateProductsAsync(ct);
            }

            _log.LogInformation(
                "Targeted reindex: {Indexed} indexed, {Deleted} deleted in {ElapsedMs}ms",
                indexed, deleted, sw.ElapsedMilliseconds);

            return new SyncResult {
                Success = true,
                DocumentsIndexed = indexed,
                DocumentsDeleted = deleted,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        } catch (Exception ex) {
            _log.LogError(ex, "Targeted reindex failed");
            return SyncResult.Failed(ex.Message);
        }
    }

    private async Task<(int indexed, int deleted)> IndexByIdsAsync(IReadOnlyCollection<long> productIds, CancellationToken ct) {
        if (productIds.Count == 0) return (0, 0);

        List<ProductSyncData> products = await _repository.GetProductsByIdsAsync(productIds);
        List<long> foundIds = products.Select(p => p.Id).ToList();
        Dictionary<long, List<string>> originalNumbers = await _repository.GetOriginalNumbersForProductsAsync(foundIds);

        List<ProductDocument> documents = products
            .Select(p => CreateDocument(p, originalNumbers.GetValueOrDefault(p.Id)))
            .ToList();

        int indexed = 0;
        foreach (ProductDocument[] batch in PartitionBulkItems(documents, _syncSettings.BatchSize)) {
            indexed += await BulkIndexAsync(batch, ct);
        }

        // Ids no longer present as live products are removed from the index.
        HashSet<long> found = foundIds.ToHashSet();
        List<long> missing = productIds.Where(id => !found.Contains(id)).ToList();
        int deleted = 0;
        foreach (long[] batch in PartitionBulkItems(missing, _syncSettings.BatchSize)) {
            deleted += await BulkDeleteAsync(batch, ct);
        }

        HttpResponseMessage refreshResponse =
            await _http.PostAsync($"{_settings.IndexName}/_refresh", null, ct);
        string refreshBody = await refreshResponse.Content.ReadAsStringAsync(ct);
        EnsureSuccessfulHttpResponse(refreshResponse, "index refresh", refreshBody);
        return (indexed, deleted);
    }

    private async Task<int> BulkIndexAsync(
        IReadOnlyCollection<ProductDocument> documents,
        CancellationToken ct,
        string? targetIndex = null) {
        if (documents.Count == 0) return 0;

        string index = targetIndex ?? _settings.IndexName;
        StringBuilder sb = new StringBuilder();
        foreach (ProductDocument doc in documents) {
            sb.AppendLine(JsonSerializer.Serialize(new { index = new { _index = index, _id = doc.Id } }, JsonOptions));
            sb.AppendLine(JsonSerializer.Serialize(doc, JsonOptions));
        }

        StringContent content = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-ndjson");
        HttpResponseMessage response = await _http.PostAsync("_bulk", content, ct);
        string responseBody = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccessfulHttpResponse(response, "bulk index", responseBody);
        return ValidateBulkResponse(responseBody, "index", documents.Count);
    }

    private async Task<int> BulkDeleteAsync(IReadOnlyCollection<long> ids, CancellationToken ct) {
        if (ids.Count == 0) return 0;

        StringBuilder sb = new StringBuilder();
        foreach (long id in ids) {
            sb.AppendLine(JsonSerializer.Serialize(new { delete = new { _index = _settings.IndexName, _id = id } }, JsonOptions));
        }

        StringContent content = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-ndjson");
        HttpResponseMessage response = await _http.PostAsync("_bulk", content, ct);
        string responseBody = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccessfulHttpResponse(response, "bulk delete", responseBody);
        return ValidateBulkResponse(responseBody, "delete", ids.Count, allowNotFound: true);
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
            Available = data.AvailableQtyUk > 0 ||
                        data.AvailableQtyUkVat > 0 ||
                        data.AvailableQtyPl > 0 ||
                        data.AvailableQtyPlVat > 0,
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
            RetailCurrencyCode = data.RetailCurrencyCode ?? "EUR",
            UpdatedAt = data.Updated
        };
    }
}
