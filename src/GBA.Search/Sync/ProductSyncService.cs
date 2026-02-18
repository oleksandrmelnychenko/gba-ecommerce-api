using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Configuration;
using GBA.Search.Models;
using GBA.Search.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Sync;

public sealed class ProductSyncService(
    ProductSyncRepository repository,
    HttpClient httpClient,
    IOptions<TypesenseSettings> typesenseSettings,
    IOptions<SyncSettings> syncSettings,
    SearchTextProcessor textProcessor,
    ILogger<ProductSyncService> logger)
    : IProductSyncService {
    private readonly TypesenseSettings _typesenseSettings = typesenseSettings.Value;
    private readonly SyncSettings _syncSettings = syncSettings.Value;

    private DateTime _lastSyncTime = DateTime.MinValue;
    private readonly Lock _syncLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task EnsureCollectionExistsAsync(string collectionName, CancellationToken cancellationToken = default) {
        SchemaField[] fields = BuildSchemaFields();

        HttpResponseMessage response = await httpClient.GetAsync(
            $"collections/{collectionName}", cancellationToken);

        if (response.IsSuccessStatusCode) {
            TypesenseCollectionSchema? existing = await response.Content.ReadFromJsonAsync<TypesenseCollectionSchema>(JsonOptions, cancellationToken);
            HashSet<string?> existingNames = existing?.Fields?
                                                 .Select(f => f.Name)
                                                 .Where(n => !string.IsNullOrWhiteSpace(n))
                                                 .ToHashSet(StringComparer.OrdinalIgnoreCase)
                                             ?? new HashSet<string?>(StringComparer.OrdinalIgnoreCase);

            SchemaField[] missing = fields
                .Where(f => !existingNames.Contains(f.Name) && f.Name != "id")
                .ToArray();

            if (missing.Length > 0) {
                var patchPayload = new { fields = missing };
                StringContent patchContent = new(
                    JsonSerializer.Serialize(patchPayload, JsonOptions),
                    Encoding.UTF8,
                    "application/json");

                using HttpResponseMessage patchResponse = await httpClient.PatchAsync(
                    $"collections/{collectionName}",
                    patchContent,
                    cancellationToken);
                patchResponse.EnsureSuccessStatusCode();

                logger.LogInformation(
                    "Patched collection '{Collection}' with {Count} missing fields",
                    collectionName, missing.Length);
            }

            logger.LogInformation("Collection '{Collection}' already exists", collectionName);
            return;
        }

        if (response.StatusCode != HttpStatusCode.NotFound) {
            response.EnsureSuccessStatusCode();
        }

        var schema = new {
            name = collectionName,
            fields,
            default_sorting_field = "updatedAt",
            token_separators = new[] { ".", "-", "/", "_" }
        };

        StringContent content = new StringContent(
            JsonSerializer.Serialize(schema, JsonOptions),
            Encoding.UTF8,
            "application/json");

        response = await httpClient.PostAsync("collections", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Created collection '{Collection}'", collectionName);
    }

    private static SchemaField[] BuildSchemaFields() {
        return [
            new SchemaField { Name = "id", Type = "string" },
            new SchemaField { Name = "netUid", Type = "string", Optional = true },
            new SchemaField { Name = "vendorCode", Type = "string", Optional = true },
            new SchemaField { Name = "vendorCodeClean", Type = "string", Infix = true, Optional = true },
            new SchemaField { Name = "name", Type = "string", Optional = true },
            new SchemaField { Name = "nameUA", Type = "string", Optional = true },
            new SchemaField { Name = "description", Type = "string", Optional = true },
            new SchemaField { Name = "descriptionUA", Type = "string", Optional = true },
            new SchemaField { Name = "nameStem", Type = "string", Optional = true },
            new SchemaField { Name = "nameUAStem", Type = "string", Optional = true },
            new SchemaField { Name = "descriptionStem", Type = "string", Optional = true },
            new SchemaField { Name = "descriptionUAStem", Type = "string", Optional = true },
            new SchemaField { Name = "fullText", Type = "string", Optional = true },
            new SchemaField { Name = "fullTextStem", Type = "string", Optional = true },
            new SchemaField { Name = "searchName", Type = "string", Infix = true, Optional = true },
            new SchemaField { Name = "searchNameUA", Type = "string", Infix = true, Optional = true },
            new SchemaField { Name = "searchDescription", Type = "string", Optional = true },
            new SchemaField { Name = "searchDescriptionUA", Type = "string", Optional = true },
            new SchemaField { Name = "searchSize", Type = "string", Optional = true },
            new SchemaField { Name = "mainOriginalNumber", Type = "string", Optional = true },
            new SchemaField { Name = "mainOriginalNumberClean", Type = "string", Infix = true, Optional = true },
            new SchemaField { Name = "originalNumbers", Type = "string[]", Optional = true },
            new SchemaField { Name = "originalNumbersClean", Type = "string[]", Infix = true, Optional = true },
            new SchemaField { Name = "size", Type = "string", Optional = true },
            new SchemaField { Name = "sizeClean", Type = "string", Infix = true, Optional = true },
            new SchemaField { Name = "synonyms", Type = "string", Optional = true },
            new SchemaField { Name = "synonymsStem", Type = "string", Optional = true },
            new SchemaField { Name = "keywords", Type = "string[]", Optional = true },
            // Product details
            new SchemaField { Name = "packingStandard", Type = "string", Optional = true },
            new SchemaField { Name = "orderStandard", Type = "string", Optional = true },
            new SchemaField { Name = "ucgfea", Type = "string", Optional = true },
            new SchemaField { Name = "volume", Type = "string", Optional = true },
            new SchemaField { Name = "top", Type = "string", Optional = true },
            new SchemaField { Name = "weight", Type = "float", Optional = true },
            new SchemaField { Name = "hasAnalogue", Type = "bool", Optional = true },
            new SchemaField { Name = "hasComponent", Type = "bool", Optional = true },
            new SchemaField { Name = "hasImage", Type = "bool", Optional = true },
            new SchemaField { Name = "image", Type = "string", Optional = true },
            new SchemaField { Name = "measureUnitId", Type = "int64", Optional = true },
            // Availability
            new SchemaField { Name = "available", Type = "bool", Facet = true },
            new SchemaField { Name = "availableQtyUk", Type = "float", Sort = true },
            new SchemaField { Name = "availableQtyUkVat", Type = "float", Optional = true },
            new SchemaField { Name = "availableQtyPl", Type = "float", Optional = true },
            new SchemaField { Name = "availableQtyPlVat", Type = "float", Optional = true },
            new SchemaField { Name = "availableQty", Type = "float", Sort = true },
            // Flags
            new SchemaField { Name = "isForWeb", Type = "bool", Facet = true },
            new SchemaField { Name = "isForSale", Type = "bool", Facet = true },
            new SchemaField { Name = "isForZeroSale", Type = "bool", Optional = true },
            // Slug
            new SchemaField { Name = "slugId", Type = "int64", Optional = true },
            new SchemaField { Name = "slugNetUid", Type = "string", Optional = true },
            new SchemaField { Name = "slugUrl", Type = "string", Optional = true },
            new SchemaField { Name = "slugLocale", Type = "string", Optional = true },
            new SchemaField { Name = "updatedAt", Type = "int64", Sort = true }
        ];
    }

    public async Task<SyncResult> IncrementalSyncAsync(CancellationToken cancellationToken = default) {
        Stopwatch sw = Stopwatch.StartNew();

        DateTime syncSince;
        lock (_syncLock) {
            syncSince = _lastSyncTime;
        }

        if (syncSince == DateTime.MinValue) {
            logger.LogInformation("First sync - performing full rebuild");
            return await FullRebuildAsync(cancellationToken);
        }

        try {
            logger.LogDebug("Starting incremental sync since {Since}", syncSince);

            List<ProductSyncData> changedProducts = await repository.GetChangedProductsAsync(syncSince);
            List<long> deletedIds = await repository.GetDeletedProductIdsAsync(syncSince);

            if (changedProducts.Count == 0 && deletedIds.Count == 0) {
                logger.LogDebug("No changes to sync");
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

            List<long> productIds = changedProducts.Select(p => p.Id).ToList();
            Dictionary<long, List<string>> originalNumbers = await repository.GetOriginalNumbersForProductsAsync(productIds);

            List<ProductSearchDocument> documents = changedProducts
                .Select(p => CreateDocument(p, originalNumbers.GetValueOrDefault(p.Id)))
                .ToList();

            string targetCollection = await ResolveTargetCollectionAsync(cancellationToken);
            await EnsureCollectionExistsAsync(targetCollection, cancellationToken);

            int indexed = await IndexDocumentsAsync(targetCollection, documents, cancellationToken);
            int deleted = await DeleteDocumentsAsync(targetCollection, deletedIds, cancellationToken);

            sw.Stop();

            lock (_syncLock) {
                _lastSyncTime = DateTime.UtcNow;
            }

            logger.LogInformation(
                "Incremental sync completed: {Indexed} indexed, {Deleted} deleted in {ElapsedMs}ms",
                indexed, deleted, sw.ElapsedMilliseconds);

            return new SyncResult {
                Success = true,
                DocumentsIndexed = indexed,
                DocumentsDeleted = deleted,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        } catch (Exception ex) {
            logger.LogError(ex, "Incremental sync failed");
            return SyncResult.Failed(ex.Message);
        }
    }

    public async Task<SyncResult> FullRebuildAsync(CancellationToken cancellationToken = default) {
        Stopwatch sw = Stopwatch.StartNew();

        try {
            logger.LogInformation("Starting full rebuild");

            string aliasName = _typesenseSettings.CollectionName;
            string targetCollection = aliasName;

            if (_syncSettings.UseAliasSwap) {
                targetCollection = BuildCollectionName(aliasName);
                await EnsureCollectionExistsAsync(targetCollection, cancellationToken);
            } else {
                await EnsureCollectionExistsAsync(aliasName, cancellationToken);
            }

            List<ProductSyncData> products = await repository.GetAllProductsAsync();
            logger.LogInformation("Fetched {Count} products from SQL", products.Count);

            List<long> productIds = products.Select(p => p.Id).ToList();
            Dictionary<long, List<string>> originalNumbers = await repository.GetOriginalNumbersForProductsAsync(productIds);
            logger.LogInformation("Fetched original numbers for {Count} products", originalNumbers.Count);

            int totalIndexed = 0;
            List<ProductSearchDocument> batch = new(_syncSettings.BatchSize);

            foreach (ProductSearchDocument doc in products.Select(product => CreateDocument(product, originalNumbers.GetValueOrDefault(product.Id))))
            {
                batch.Add(doc);

                if (batch.Count < _syncSettings.BatchSize) continue;
                int indexed = await IndexDocumentsAsync(targetCollection, batch, cancellationToken);
                totalIndexed += indexed;
                batch.Clear();

                logger.LogDebug("Indexed batch, total: {Total}", totalIndexed);
            }

            if (batch.Count > 0) {
                int indexed = await IndexDocumentsAsync(targetCollection, batch, cancellationToken);
                totalIndexed += indexed;
            }

            if (_syncSettings.UseAliasSwap) {
                await UpsertAliasAsync(aliasName, targetCollection, cancellationToken);
                if (_syncSettings.CleanupOldCollections) {
                    await CleanupOldCollectionsAsync(aliasName, targetCollection, cancellationToken);
                }
            }

            sw.Stop();

            lock (_syncLock) {
                _lastSyncTime = DateTime.UtcNow;
            }

            logger.LogInformation(
                "Full rebuild completed: {Total} documents indexed in {ElapsedMs}ms",
                totalIndexed, sw.ElapsedMilliseconds);

            return new SyncResult {
                Success = true,
                DocumentsIndexed = totalIndexed,
                DocumentsDeleted = 0,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        } catch (Exception ex) {
            logger.LogError(ex, "Full rebuild failed");
            return SyncResult.Failed(ex.Message);
        }
    }

    private ProductSearchDocument CreateDocument(ProductSyncData data, List<string>? origNumbers) {
        origNumbers ??= [];

        List<string> keywords = BuildKeywords(data, origNumbers);

        return new ProductSearchDocument {
            Id = data.Id.ToString(),
            NetUid = data.NetUid.ToString(),
            VendorCode = data.VendorCode,
            VendorCodeClean = NumberNormalizer.Normalize(data.SearchVendorCode),

            Name = data.Name,
            NameUA = data.NameUA,
            Description = data.Description,
            DescriptionUA = data.DescriptionUA,
            NameStem = textProcessor.StemText(data.Name),
            NameUAStem = textProcessor.StemText(data.NameUA),
            DescriptionStem = textProcessor.StemText(data.Description),
            DescriptionUAStem = textProcessor.StemText(data.DescriptionUA),

            FullText = string.Join(" ", data.NameUA, data.Name, data.DescriptionUA, data.Description).Trim(),
            FullTextStem = textProcessor.StemText(string.Join(" ", data.NameUA, data.Name, data.DescriptionUA, data.Description)),

            SearchName = data.SearchName,
            SearchNameUA = data.SearchNameUA,
            SearchDescription = data.SearchDescription,
            SearchDescriptionUA = data.SearchDescriptionUA,
            SearchSize = data.SearchSize,

            MainOriginalNumber = data.MainOriginalNumber,
            MainOriginalNumberClean = NumberNormalizer.Normalize(data.MainOriginalNumber),
            OriginalNumbers = origNumbers,
            OriginalNumbersClean = origNumbers.Select(NumberNormalizer.Normalize).Distinct().ToList(),

            Size = data.Size,
            SizeClean = NumberNormalizer.Normalize(data.Size),
            Synonyms = data.Synonyms,
            SynonymsStem = textProcessor.StemText(data.Synonyms),
            Keywords = keywords,

            // Product details
            PackingStandard = data.PackingStandard,
            OrderStandard = data.OrderStandard,
            Ucgfea = data.Ucgfea,
            Volume = data.Volume,
            Top = data.Top,
            Weight = data.Weight,
            HasAnalogue = data.HasAnalogue,
            HasComponent = data.HasComponent,
            HasImage = data.HasImage,
            Image = data.Image,
            MeasureUnitId = data.MeasureUnitId,

            // Availability
            Available = data.AvailableQty > 0,
            AvailableQtyUk = data.AvailableQtyUk + data.AvailableQtyUkVat,
            AvailableQtyUkVat = data.AvailableQtyUkVat,
            AvailableQtyPl = data.AvailableQtyPl + data.AvailableQtyPlVat,
            AvailableQtyPlVat = data.AvailableQtyPlVat,
            AvailableQty = data.AvailableQty,

            // Flags
            IsForWeb = data.IsForWeb,
            IsForSale = data.IsForSale,
            IsForZeroSale = data.IsForZeroSale,

            // Slug
            SlugId = data.SlugId,
            SlugNetUid = data.SlugNetUid.ToString(),
            SlugUrl = data.SlugUrl,
            SlugLocale = data.SlugLocale,

            // Retail pricing
            RetailPrice = data.RetailPrice,
            RetailPriceVat = data.RetailPriceVat,
            RetailCurrencyCode = data.RetailCurrencyCode,

            UpdatedAt = new DateTimeOffset(data.Updated).ToUnixTimeSeconds()
        };
    }

    private List<string> BuildKeywords(ProductSyncData data, List<string> origNumbers) {
        const int maxKeywords = 64;
        HashSet<string> tokens = new(StringComparer.Ordinal);

        AddTokens(tokens, data.Name);
        AddTokens(tokens, data.NameUA);
        AddTokens(tokens, data.Description);
        AddTokens(tokens, data.DescriptionUA);
        AddTokens(tokens, data.Synonyms);

        AddToken(tokens, data.VendorCode);
        AddToken(tokens, NumberNormalizer.Normalize(data.VendorCode));
        AddToken(tokens, data.SearchVendorCode);
        AddToken(tokens, data.SearchSize);
        AddToken(tokens, data.MainOriginalNumber);
        AddToken(tokens, NumberNormalizer.Normalize(data.MainOriginalNumber));

        foreach (string number in origNumbers) {
            AddToken(tokens, number);
            AddToken(tokens, NumberNormalizer.Normalize(number));
        }

        return tokens.Where(t => !string.IsNullOrWhiteSpace(t)).Take(maxKeywords).ToList();
    }

    private void AddTokens(HashSet<string> tokens, string? value) {
        foreach (string token in textProcessor.Tokenize(value)) {
            AddToken(tokens, token);
        }
    }

    private static void AddToken(HashSet<string> tokens, string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return;
        }
        tokens.Add(value);
    }

    private async Task<int> IndexDocumentsAsync(
        string collectionName,
        List<ProductSearchDocument> documents,
        CancellationToken cancellationToken) {

        if (documents.Count == 0) return 0;

        StringBuilder sb = new();
        foreach (ProductSearchDocument doc in documents) {
            sb.AppendLine(JsonSerializer.Serialize(doc, JsonOptions));
        }

        StringContent content = new(sb.ToString(), Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync(
            $"collections/{collectionName}/documents/import?action=upsert",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return documents.Count;
    }

    private async Task<int> DeleteDocumentsAsync(
        string collectionName,
        List<long> productIds,
        CancellationToken cancellationToken) {

        if (productIds.Count == 0) return 0;

        int deleted = 0;

        foreach (long id in productIds) {
            try {
                HttpResponseMessage response = await httpClient.DeleteAsync(
                    $"collections/{collectionName}/documents/{id}",
                    cancellationToken);

                if (response.IsSuccessStatusCode) {
                    deleted++;
                }
            } catch (Exception ex) {
                logger.LogWarning(ex, "Failed to delete document {Id}", id);
            }
        }

        return deleted;
    }

    private async Task<string> ResolveTargetCollectionAsync(CancellationToken cancellationToken) {
        if (!_syncSettings.UseAliasSwap) {
            return _typesenseSettings.CollectionName;
        }

        string aliasName = _typesenseSettings.CollectionName;
        string? resolved = await TryResolveAliasTargetAsync(aliasName, cancellationToken);

        return string.IsNullOrWhiteSpace(resolved) ? aliasName : resolved;
    }

    private static string BuildCollectionName(string aliasName) {
        return $"{aliasName}_{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private async Task<string?> TryResolveAliasTargetAsync(string aliasName, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await httpClient.GetAsync($"aliases/{aliasName}", cancellationToken);

        if (response.IsSuccessStatusCode) {
            TypesenseAliasResponse? alias = await response.Content.ReadFromJsonAsync<TypesenseAliasResponse>(JsonOptions, cancellationToken);
            return alias?.CollectionName;
        }

        if (response.StatusCode == HttpStatusCode.NotFound) {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return null;
    }

    private async Task UpsertAliasAsync(string aliasName, string collectionName, CancellationToken cancellationToken) {
        var payload = new { collection_name = collectionName };
        StringContent content = new(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await httpClient.PutAsync($"aliases/{aliasName}", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Alias '{Alias}' now points to '{Collection}'", aliasName, collectionName);
    }

    private async Task CleanupOldCollectionsAsync(
        string aliasName,
        string currentCollection,
        CancellationToken cancellationToken) {

        List<string> collections = await GetCollectionsAsync(cancellationToken);
        string prefix = aliasName + "_";
        int keep = Math.Max(_syncSettings.CollectionsToKeep, 0);

        List<string> candidates = collections
            .Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Where(c => !string.Equals(c, currentCollection, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c)
            .ToList();

        List<string> toDelete = candidates.Skip(keep).ToList();
        foreach (string collection in toDelete) {
            await DeleteCollectionAsync(collection, cancellationToken);
        }
    }

    private async Task<List<string>> GetCollectionsAsync(CancellationToken cancellationToken) {
        using HttpResponseMessage response = await httpClient.GetAsync("collections", cancellationToken);
        response.EnsureSuccessStatusCode();

        List<TypesenseCollectionInfo>? collections = await response.Content.ReadFromJsonAsync<List<TypesenseCollectionInfo>>(
            JsonOptions, cancellationToken);

        return collections?.Select(c => c.Name ?? string.Empty)
               .Where(name => !string.IsNullOrWhiteSpace(name))
               .ToList() ?? [];
    }

    private async Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await httpClient.DeleteAsync($"collections/{collectionName}", cancellationToken);
        if (response.IsSuccessStatusCode) {
            logger.LogInformation("Deleted old collection '{Collection}'", collectionName);
        } else {
            logger.LogWarning("Failed to delete collection '{Collection}', status {Status}",
                collectionName, response.StatusCode);
        }
    }

    private sealed class TypesenseCollectionSchema {
        [JsonPropertyName("fields")]
        public List<TypesenseFieldInfo>? Fields { get; set; }
    }

    private sealed class TypesenseFieldInfo {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class SchemaField {
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public bool? Infix { get; init; }
        public bool? Facet { get; init; }
        public bool? Sort { get; init; }
        public bool? Optional { get; init; }
    }

    private sealed class TypesenseAliasResponse {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("collection_name")]
        public string? CollectionName { get; set; }
    }

    private sealed class TypesenseCollectionInfo {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
