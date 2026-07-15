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
using GBA.Services.Services.Products;
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
    private static readonly TimeSpan RebuildLeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RebuildLeaseRenewInterval = TimeSpan.FromMinutes(1);
    private const int MaxTargetedProductIds = 10_000;

    private readonly HttpClient _http;
    private readonly ElasticsearchSettings _settings;
    private readonly SyncSettings _syncSettings;
    private readonly IProductSyncRepository _repository;
    private readonly IElasticsearchIndexService _indexService;
    private readonly ISearchSyncStateStore _state;
    private readonly ILogger<ElasticsearchSyncService> _log;

    // Re-scan a small window before the last watermark so rows written during the previous
    // run are never missed (bulk upserts are idempotent, so overlap is harmless).
    private const int WatermarkOverlapSeconds = 120;

    // Process-wide single-flight: never let a rebuild and an incremental run overlap.
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ElasticsearchSyncService(
        HttpClient httpClient,
        IOptions<ElasticsearchSettings> settings,
        IOptions<SyncSettings> syncSettings,
        IProductSyncRepository repository,
        IElasticsearchIndexService indexService,
        ISearchSyncStateStore state,
        ILogger<ElasticsearchSyncService> logger) {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = (settings ?? throw new ArgumentNullException(nameof(settings))).Value;
        SearchSyncStorage.ValidateBaseIndexName(_settings.IndexName);
        _syncSettings = (syncSettings ?? throw new ArgumentNullException(nameof(syncSettings))).Value;
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<SyncResult> FullRebuildAsync(CancellationToken ct = default) =>
        RunGenerationSyncAsync(GenerationSyncMode.Full, null, ct);

    public Task<SyncResult> IncrementalSyncAsync(CancellationToken ct = default) =>
        RunGenerationSyncAsync(GenerationSyncMode.Incremental, null, ct);

    public Task<SyncResult> ReindexProductsAsync(
        IReadOnlyCollection<long> productIds,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(productIds);
        if (productIds.Count > MaxTargetedProductIds || productIds.Any(id => id <= 0)) {
            return Task.FromResult(SyncResult.Failed(
                $"Targeted reindex accepts 1-{MaxTargetedProductIds} positive product IDs."));
        }

        long[] uniqueProductIds = productIds.Distinct().ToArray();
        return uniqueProductIds.Length == 0
            ? Task.FromResult(new SyncResult { Success = true })
            : RunGenerationSyncAsync(GenerationSyncMode.Targeted, uniqueProductIds, ct);
    }

    private async Task<SyncResult> RunGenerationSyncAsync(
        GenerationSyncMode requestedMode,
        IReadOnlyCollection<long>? targetedProductIds,
        CancellationToken ct) {
        DateTime runStart = DateTime.UtcNow;
        Stopwatch stopwatch = Stopwatch.StartNew();
        SearchRebuildLease? lease = null;
        CancellationTokenSource? heartbeatStop = null;
        CancellationTokenSource? leaseLost = null;
        Task? heartbeatTask = null;
        string? stagingIndex = null;
        bool committed = false;

        await _gate.WaitAsync(ct);
        try {
            lease = await _state.TryAcquireWriteLeaseAsync(RebuildLeaseDuration, ct);
            if (lease == null) {
                return SyncResult.Failed("Another replica owns the shared search write lease");
            }

            SearchActiveGeneration? active = await _state.GetActiveGenerationAsync(ct);
            if ((active?.Generation ?? 0) != lease.ExpectedGeneration
                || !string.Equals(
                    active?.IndexName,
                    lease.ExpectedActiveIndex,
                    StringComparison.Ordinal)) {
                return SyncResult.Failed("Search generation changed while acquiring the write lease");
            }

            SearchSyncState currentState = active?.State ?? SearchSyncState.Empty;
            RetailConfigurationSnapshot configuration =
                await _repository.GetRetailConfigurationSnapshotAsync();
            if (!configuration.IsValid || string.IsNullOrWhiteSpace(configuration.Signature)) {
                return SyncResult.Failed("Retail pricing configuration is invalid or empty");
            }

            PricingDependencyRevisions pricingRevisions =
                await _repository.GetPricingDependencyRevisionsAsync();
            if (!pricingRevisions.IsValid) {
                return SyncResult.Failed(
                    "Pricing Change Tracking is unavailable; indexed retail prices were not rebuilt");
            }

            SearchRebuildLease? configurationLease =
                await _state.BindWriteLeaseConfigurationAsync(
                    lease,
                    configuration.Signature,
                    RebuildLeaseDuration,
                    ct);
            if (configurationLease == null) {
                return SyncResult.Failed(
                    "Search configuration could not be bound to the current promotion fence");
            }
            lease = configurationLease;

            GenerationSyncMode mode = requestedMode;
            bool activeRequiresFull = active == null
                                      || currentState.RequiresFullRebuild(SearchIndexSchema.CurrentVersion)
                                      || !active.HasExactIndexedPricingRevisions(pricingRevisions)
                                      || !string.Equals(
                                          currentState.RetailConfigurationSignature,
                                          configuration.Signature,
                                          StringComparison.Ordinal);
            if (mode == GenerationSyncMode.Incremental && activeRequiresFull) {
                mode = GenerationSyncMode.Full;
            } else if (mode == GenerationSyncMode.Targeted && activeRequiresFull) {
                return SyncResult.Failed(
                    "Targeted reindex requires a current active generation; run a full rebuild first");
            }

            if (!await _indexService.ValidateConfiguredNameModeAsync(
                    _syncSettings.UseAliasSwap,
                    ct)) {
                return SyncResult.Failed(
                    "Elasticsearch configured name conflicts with UseAliasSwap; no migration was attempted");
            }

            stagingIndex = await _indexService.CreateVersionedIndexAsync(lease, ct);
            if (string.IsNullOrWhiteSpace(stagingIndex)) {
                return SyncResult.Failed("Failed to create a staging search generation");
            }
            EnsureStagingIndex(stagingIndex);

            if (!await _state.RenewWriteLeaseAsync(
                    lease,
                    stagingIndex,
                    RebuildLeaseDuration,
                    ct)) {
                return SyncResult.Failed("Lost the shared search write lease before staging");
            }

            heartbeatStop = CancellationTokenSource.CreateLinkedTokenSource(ct);
            leaseLost = new CancellationTokenSource();
            heartbeatTask = MaintainWriteLeaseAsync(
                lease,
                stagingIndex,
                leaseLost,
                heartbeatStop.Token);
            using CancellationTokenSource operationCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(ct, leaseLost.Token);
            CancellationToken operationCt = operationCancellation.Token;

            if (mode != GenerationSyncMode.Full) {
                if (active == null
                    || !await _indexService.CloneGenerationAsync(
                        lease,
                        active.IndexName,
                        stagingIndex,
                        operationCt)) {
                    return SyncResult.Failed("Failed to clone the active search generation");
                }
            }

            (int indexed, int deleted) = mode switch {
                GenerationSyncMode.Full => await BuildFullGenerationAsync(
                    lease,
                    stagingIndex,
                    configuration.Signature,
                    pricingRevisions,
                    operationCt),
                GenerationSyncMode.Incremental => await ApplyIncrementalGenerationAsync(
                    lease,
                    stagingIndex,
                    currentState,
                    configuration.Signature,
                    pricingRevisions,
                    operationCt),
                GenerationSyncMode.Targeted => await ApplyTargetedGenerationAsync(
                    lease,
                    stagingIndex,
                    targetedProductIds!,
                    configuration.Signature,
                    pricingRevisions,
                    operationCt),
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };

            if (!await _indexService.RefreshGenerationAsync(lease, stagingIndex, operationCt)) {
                return SyncResult.Failed("Failed to refresh the fenced staging generation");
            }

            if (!await _repository.IsRetailConfigurationCurrentAsync(configuration.Signature)) {
                return SyncResult.Failed(
                    "Retail pricing configuration changed before generation promotion; live generation was preserved");
            }

            if (!pricingRevisions.MatchesExactly(
                    await _repository.GetPricingDependencyRevisionsAsync())) {
                return SyncResult.Failed(
                    "Pricing Change Tracking advanced during generation build; live generation was preserved");
            }

            bool aliasCutOver = false;
            if (_syncSettings.UseAliasSwap) {
                aliasCutOver = await _indexService.SwapAliasAsync(lease, stagingIndex, operationCt);
                if (!aliasCutOver) {
                    return SyncResult.Failed("Failed to atomically cut over the fenced search alias");
                }
            }

            if (!await _repository.IsRetailConfigurationCurrentAsync(configuration.Signature)) {
                if (aliasCutOver) {
                    await RestoreAliasAfterFailedPromotionAsync(lease, stagingIndex);
                }
                return SyncResult.Failed(
                    "Retail pricing configuration changed during generation cutover; active generation was preserved");
            }

            if (!pricingRevisions.MatchesExactly(
                    await _repository.GetPricingDependencyRevisionsAsync())) {
                if (aliasCutOver) {
                    await RestoreAliasAfterFailedPromotionAsync(lease, stagingIndex);
                }
                return SyncResult.Failed(
                    "Pricing Change Tracking advanced during generation cutover; active generation was preserved");
            }

            DateTime watermark = mode == GenerationSyncMode.Targeted
                ? currentState.WatermarkUtc
                : runStart;
            SearchGenerationAcknowledgement? acknowledgement;
            try {
                acknowledgement = await _state.PromoteGenerationAsync(
                    lease,
                    stagingIndex,
                    watermark,
                    SearchIndexSchema.CurrentVersion,
                    mode == GenerationSyncMode.Full ? DateTime.UtcNow : null,
                    currentState.RetailConfigurationSignature,
                    configuration.Signature,
                    pricingRevisions,
                    operationCt);
            } catch (Exception ex) {
                PromotionResolution resolution = await ObservePromotionResolutionAsync(
                    lease,
                    stagingIndex,
                    configuration.Signature,
                    pricingRevisions);
                if (resolution == PromotionResolution.Promoted) {
                    _log.LogWarning(
                        ex,
                        "Promotion response failed after durable generation {Index} was committed; continuing from verified state",
                        stagingIndex);
                    acknowledgement = ExpectedAcknowledgement(lease, stagingIndex);
                } else {
                    if (resolution == PromotionResolution.PreviousGeneration && aliasCutOver) {
                        await RestoreAliasAfterFailedPromotionAsync(lease, stagingIndex);
                    } else if (aliasCutOver) {
                        _log.LogCritical(
                            ex,
                            "Promotion outcome for {Index} is unknown; readiness will fail closed until operator recovery",
                            stagingIndex);
                    }

                    throw;
                }
            }

            if (acknowledgement == null
                || acknowledgement.OwnerId != lease.OwnerId
                || acknowledgement.FencingToken != lease.FencingToken
                || !string.Equals(
                    acknowledgement.IndexName,
                    stagingIndex,
                    StringComparison.Ordinal)
                || acknowledgement.Generation != lease.ExpectedGeneration + 1) {
                PromotionResolution resolution = await ObservePromotionResolutionAsync(
                    lease,
                    stagingIndex,
                    configuration.Signature,
                    pricingRevisions);
                if (resolution == PromotionResolution.Promoted) {
                    acknowledgement = ExpectedAcknowledgement(lease, stagingIndex);
                } else {
                    if (resolution == PromotionResolution.PreviousGeneration && aliasCutOver) {
                        await RestoreAliasAfterFailedPromotionAsync(lease, stagingIndex);
                    } else if (aliasCutOver) {
                        _log.LogCritical(
                            "Promotion acknowledgement for {Index} is invalid and durable state is ambiguous; readiness will fail closed",
                            stagingIndex);
                    }

                    return SyncResult.Failed(
                        "Search generation promotion was not acknowledged for the current fenced owner");
                }
            }

            committed = true;
            stopwatch.Stop();
            _log.LogInformation(
                "Search {Mode} generation {Generation} promoted: {Indexed} indexed, {Deleted} deleted in {ElapsedMs}ms",
                mode,
                acknowledgement.Generation,
                indexed,
                deleted,
                stopwatch.ElapsedMilliseconds);

            if (_syncSettings.CleanupOldCollections) {
                try {
                    await _indexService.CleanupOldVersionedIndicesAsync(
                        lease,
                        _syncSettings.CollectionsToKeep,
                        operationCt);
                } catch (OperationCanceledException ex) {
                    _log.LogWarning(
                        ex,
                        "Generation promotion succeeded, but fenced old-index cleanup was cancelled");
                } catch (Exception ex) {
                    _log.LogWarning(ex, "Generation promotion succeeded, but old-index cleanup failed");
                }
            }

            return new SyncResult {
                Success = true,
                DocumentsIndexed = indexed,
                DocumentsDeleted = deleted,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) when (leaseLost?.IsCancellationRequested == true) {
            _log.LogError("Search generation build stopped after losing its distributed write lease");
            return SyncResult.Failed("Lost the shared search write lease while building a generation");
        } catch (Exception ex) {
            _log.LogError(ex, "Search {Mode} generation failed", requestedMode);
            return SyncResult.Failed(ex.Message);
        } finally {
            await StopHeartbeatAsync(heartbeatStop, heartbeatTask);
            heartbeatStop?.Dispose();

            if (lease != null && stagingIndex != null && !committed) {
                try {
                    await _indexService.DeleteFailedVersionedIndexAsync(
                        lease,
                        stagingIndex,
                        CancellationToken.None);
                } catch (Exception ex) {
                    _log.LogWarning(
                        ex,
                        "Failed to clean up uncommitted staging generation {Index}",
                        stagingIndex);
                }
            }

            if (lease != null) {
                await _state.ReleaseWriteLeaseAsync(lease, CancellationToken.None);
            }

            leaseLost?.Dispose();
            _gate.Release();
        }
    }

    private async Task<(int indexed, int deleted)> BuildFullGenerationAsync(
        SearchRebuildLease lease,
        string stagingIndex,
        string expectedConfigurationSignature,
        PricingDependencyRevisions pricingRevisions,
        CancellationToken ct) {
        int indexed = 0;
        long afterProductId = 0;
        int projectionBatchSize = Math.Clamp(_syncSettings.BatchSize, 1, 2000);

        while (true) {
            ProductProjectionBatch projection = await _repository.GetProductProjectionBatchAsync(
                afterProductId,
                projectionBatchSize,
                expectedConfigurationSignature);
            if (!projection.HasValidRetailConfiguration) {
                throw new InvalidOperationException(
                    "Retail pricing configuration changed during full projection; live generation was preserved.");
            }
            if (projection.ScannedCount == 0) break;
            if (projection.LastScannedProductId <= afterProductId) {
                throw new InvalidOperationException("Product projection keyset did not advance.");
            }

            indexed += await IndexProductsAsync(
                lease,
                projection.Products,
                stagingIndex,
                pricingRevisions,
                ct);
            afterProductId = projection.LastScannedProductId;
            if (!projection.HasMore) break;
        }

        if (indexed == 0) {
            throw new InvalidOperationException(
                "Full rebuild projected no eligible products; live generation was preserved.");
        }

        return (indexed, 0);
    }

    private async Task<(int indexed, int deleted)> ApplyIncrementalGenerationAsync(
        SearchRebuildLease lease,
        string stagingIndex,
        SearchSyncState currentState,
        string expectedConfigurationSignature,
        PricingDependencyRevisions pricingRevisions,
        CancellationToken ct) {
        DateTime since = currentState.WatermarkUtc.AddSeconds(-WatermarkOverlapSeconds);
        int idBatchSize = Math.Clamp(_syncSettings.BatchSize, 1, 2000);
        int indexed = 0;
        int deleted = 0;
        long afterChangedId = 0;

        while (true) {
            ProductIdSyncBatch batch = await _repository.GetChangedProductIdBatchAsync(
                since,
                currentState.RetailConfigurationSignature,
                afterChangedId,
                idBatchSize);
            if (!batch.HasValidRetailConfiguration || batch.RequiresFullReconciliation) {
                throw new InvalidOperationException(
                    "Retail pricing configuration changed during incremental planning; live generation was preserved.");
            }
            if (batch.ProductIds.Count == 0) break;
            if (batch.LastScannedProductId <= afterChangedId) {
                throw new InvalidOperationException("Incremental product keyset did not advance.");
            }

            (int batchIndexed, int batchDeleted) = await ApplyProductIdsAsync(
                lease,
                batch.ProductIds,
                stagingIndex,
                expectedConfigurationSignature,
                pricingRevisions,
                ct);
            indexed += batchIndexed;
            deleted += batchDeleted;
            afterChangedId = batch.LastScannedProductId;
            if (!batch.HasMore) break;
        }

        long afterDeletedId = 0;
        while (true) {
            List<long> deletedIds = await _repository.GetDeletedProductIdBatchAsync(
                since,
                afterDeletedId,
                idBatchSize);
            if (deletedIds.Count == 0) break;
            deleted += await BulkDeleteInBatchesAsync(lease, deletedIds, stagingIndex, ct);
            afterDeletedId = deletedIds[^1];
            if (deletedIds.Count < idBatchSize) break;
        }

        return (indexed, deleted);
    }

    private async Task<(int indexed, int deleted)> ApplyTargetedGenerationAsync(
        SearchRebuildLease lease,
        string stagingIndex,
        IReadOnlyCollection<long> productIds,
        string expectedConfigurationSignature,
        PricingDependencyRevisions pricingRevisions,
        CancellationToken ct) {
        int indexed = 0;
        int deleted = 0;
        int idBatchSize = Math.Clamp(_syncSettings.BatchSize, 1, 2000);
        foreach (long[] batch in productIds.Chunk(idBatchSize)) {
            (int batchIndexed, int batchDeleted) = await ApplyProductIdsAsync(
                lease,
                batch,
                stagingIndex,
                expectedConfigurationSignature,
                pricingRevisions,
                ct);
            indexed += batchIndexed;
            deleted += batchDeleted;
        }

        return (indexed, deleted);
    }

    private async Task<(int indexed, int deleted)> ApplyProductIdsAsync(
        SearchRebuildLease lease,
        IReadOnlyCollection<long> productIds,
        string stagingIndex,
        string expectedConfigurationSignature,
        PricingDependencyRevisions pricingRevisions,
        CancellationToken ct) {
        ProductProjectionSnapshot projection = await _repository.GetProductProjectionByIdsAsync(
            productIds,
            expectedConfigurationSignature);
        if (!projection.HasValidRetailConfiguration
            || !string.Equals(
                projection.RetailConfigurationSignature,
                expectedConfigurationSignature,
                StringComparison.Ordinal)) {
            throw new InvalidOperationException(
                "Retail pricing configuration changed during targeted projection; live generation was preserved.");
        }

        int indexed = await IndexProductsAsync(
            lease,
            projection.Products,
            stagingIndex,
            pricingRevisions,
            ct);
        HashSet<long> foundIds = projection.Products.Select(product => product.Id).ToHashSet();
        List<long> missingIds = productIds.Where(id => !foundIds.Contains(id)).ToList();
        int deleted = await BulkDeleteInBatchesAsync(lease, missingIds, stagingIndex, ct);
        return (indexed, deleted);
    }

    private async Task<int> IndexProductsAsync(
        SearchRebuildLease lease,
        IReadOnlyCollection<ProductSyncData> products,
        string stagingIndex,
        PricingDependencyRevisions pricingRevisions,
        CancellationToken ct) {
        if (products.Count == 0) return 0;

        List<long> productIds = products.Select(product => product.Id).ToList();
        Dictionary<long, List<string>> originalNumbers =
            await _repository.GetOriginalNumbersForProductsAsync(productIds);
        List<ProductDocument> documents = products
            .Select(product => CreateDocument(
                product,
                originalNumbers.GetValueOrDefault(product.Id),
                pricingRevisions))
            .ToList();

        int indexed = 0;
        int bulkBatchSize = Math.Clamp(_syncSettings.BatchSize, 1, 2000);
        foreach (ProductDocument[] batch in documents.Chunk(bulkBatchSize)) {
            indexed += await BulkIndexAsync(lease, batch, stagingIndex, ct);
        }
        return indexed;
    }

    private async Task<int> BulkIndexAsync(
        SearchRebuildLease lease,
        IReadOnlyCollection<ProductDocument> documents,
        string targetIndex,
        CancellationToken ct) {
        if (documents.Count == 0) return 0;
        EnsureStagingIndex(targetIndex);
        await EnsureCurrentWriteFenceAsync(lease, targetIndex, ct);

        StringBuilder sb = new StringBuilder();
        foreach (ProductDocument doc in documents) {
            sb.AppendLine(JsonSerializer.Serialize(
                new { index = new { _index = targetIndex, _id = doc.Id } },
                JsonOptions));
            sb.AppendLine(JsonSerializer.Serialize(doc, JsonOptions));
        }

        StringContent content = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-ndjson");
        using HttpResponseMessage response = await _http.PostAsync("_bulk", content, ct);
        return await ValidateBulkResponseAsync(response, "index", documents.Count, allowNotFound: false, ct);
    }

    private async Task<int> BulkDeleteInBatchesAsync(
        SearchRebuildLease lease,
        IReadOnlyCollection<long> ids,
        string targetIndex,
        CancellationToken ct) {
        if (ids.Count == 0) return 0;
        int deleted = 0;
        int batchSize = Math.Clamp(_syncSettings.BatchSize, 1, 2000);
        foreach (long[] batch in ids.Chunk(batchSize)) {
            deleted += await BulkDeleteAsync(lease, batch, targetIndex, ct);
        }
        return deleted;
    }

    private async Task<int> BulkDeleteAsync(
        SearchRebuildLease lease,
        IReadOnlyCollection<long> ids,
        string targetIndex,
        CancellationToken ct) {
        if (ids.Count == 0) return 0;
        EnsureStagingIndex(targetIndex);
        await EnsureCurrentWriteFenceAsync(lease, targetIndex, ct);

        StringBuilder sb = new StringBuilder();
        foreach (long id in ids) {
            sb.AppendLine(JsonSerializer.Serialize(
                new { delete = new { _index = targetIndex, _id = id } },
                JsonOptions));
        }

        StringContent content = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-ndjson");
        using HttpResponseMessage response = await _http.PostAsync("_bulk", content, ct);
        return await ValidateBulkResponseAsync(response, "delete", ids.Count, allowNotFound: true, ct);
    }

    private async Task EnsureCurrentWriteFenceAsync(
        SearchRebuildLease lease,
        string stagingIndex,
        CancellationToken ct) {
        if (!await _state.ValidateWriteLeaseAsync(lease, stagingIndex, ct)) {
            throw new InvalidOperationException(
                "Search index mutation was rejected because the coordinator lease or configuration fence is stale.");
        }
    }

    private async Task<int> ValidateBulkResponseAsync(
        HttpResponseMessage response,
        string actionName,
        int expectedCount,
        bool allowNotFound,
        CancellationToken ct) {
        await EnsureSuccessAsync(response, $"bulk {actionName}", ct);

        string responseBody = await response.Content.ReadAsStringAsync(ct);
        using JsonDocument jsonDoc = JsonDocument.Parse(responseBody);
        JsonElement root = jsonDoc.RootElement;

        if (!root.TryGetProperty("errors", out JsonElement errorsElement)
            || errorsElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
            || !root.TryGetProperty("items", out JsonElement items)
            || items.ValueKind != JsonValueKind.Array) {
            throw new InvalidOperationException($"Elasticsearch bulk {actionName} response was incomplete.");
        }

        JsonElement.ArrayEnumerator itemEnumerator = items.EnumerateArray();
        int itemCount = items.GetArrayLength();
        if (itemCount != expectedCount) {
            throw new InvalidOperationException(
                $"Elasticsearch bulk {actionName} returned {itemCount} results for {expectedCount} operations.");
        }

        List<string> errors = [];
        bool toleratedNotFound = false;
        foreach (JsonElement item in itemEnumerator) {
            if (!item.TryGetProperty(actionName, out JsonElement result)
                || !result.TryGetProperty("status", out JsonElement statusElement)
                || !statusElement.TryGetInt32(out int status)) {
                errors.Add("missing action result or status");
                continue;
            }

            bool notFoundIsIdempotent = allowNotFound && status == 404;
            toleratedNotFound |= notFoundIsIdempotent;
            bool successful = status is >= 200 and < 300 || notFoundIsIdempotent;
            if (!successful) {
                string detail = result.TryGetProperty("error", out JsonElement error)
                    ? error.ToString()
                    : $"status {status}";
                errors.Add(detail);
            }
        }

        if (errors.Count > 0) {
            string sample = string.Join("; ", errors.Take(3));
            throw new InvalidOperationException(
                $"Elasticsearch bulk {actionName} failed for {errors.Count} of {expectedCount} operations: {sample}");
        }

        if (errorsElement.GetBoolean() && !toleratedNotFound) {
            throw new InvalidOperationException(
                $"Elasticsearch bulk {actionName} reported errors despite successful item statuses.");
        }

        return expectedCount;
    }

    private async Task RestoreAliasAfterFailedPromotionAsync(
        SearchRebuildLease lease,
        string stagingIndex) {
        try {
            if (!await _indexService.RestoreAliasAsync(
                    lease,
                    stagingIndex,
                    CancellationToken.None)) {
                _log.LogCritical(
                    "Alias rollback for uncommitted generation {Index} failed; readiness must remain unhealthy until recovery",
                    stagingIndex);
            }
        } catch (Exception ex) {
            _log.LogCritical(
                ex,
                "Alias rollback for uncommitted generation {Index} threw; readiness must remain unhealthy until recovery",
                stagingIndex);
        }
    }

    private async Task<PromotionResolution> ObservePromotionResolutionAsync(
        SearchRebuildLease lease,
        string stagingIndex,
        string configurationSignature,
        PricingDependencyRevisions pricingRevisions) {
        try {
            SearchActiveGeneration? active = await _state.GetActiveGenerationAsync(CancellationToken.None);
            if (active != null
                && active.Generation == lease.ExpectedGeneration + 1
                && string.Equals(active.IndexName, stagingIndex, StringComparison.Ordinal)
                && active.HasConsistentConfiguration
                && string.Equals(
                    active.State.RetailConfigurationSignature,
                    configurationSignature,
                    StringComparison.Ordinal)
                && active.State.RetailConfigurationEpoch == lease.ConfigurationEpoch
                && active.HasExactIndexedPricingRevisions(pricingRevisions)) {
                return PromotionResolution.Promoted;
            }

            if ((active?.Generation ?? 0) == lease.ExpectedGeneration
                && string.Equals(
                    active?.IndexName,
                    lease.ExpectedActiveIndex,
                    StringComparison.Ordinal)) {
                return PromotionResolution.PreviousGeneration;
            }
        } catch (Exception ex) {
            _log.LogCritical(
                ex,
                "Could not resolve durable promotion state for generation {Index}",
                stagingIndex);
        }

        return PromotionResolution.Unknown;
    }

    private static SearchGenerationAcknowledgement ExpectedAcknowledgement(
        SearchRebuildLease lease,
        string stagingIndex) {
        return new SearchGenerationAcknowledgement(
            lease.OwnerId,
            lease.FencingToken,
            stagingIndex,
            lease.ExpectedGeneration + 1);
    }

    private async Task MaintainWriteLeaseAsync(
        SearchRebuildLease lease,
        string stagingIndex,
        CancellationTokenSource leaseLost,
        CancellationToken ct) {
        try {
            while (true) {
                await Task.Delay(RebuildLeaseRenewInterval, ct);
                if (!await _state.RenewWriteLeaseAsync(
                        lease,
                        stagingIndex,
                        RebuildLeaseDuration,
                        ct)) {
                    _log.LogError(
                        "Search write lease ownership was lost for staging generation {Index}",
                        stagingIndex);
                    leaseLost.Cancel();
                    return;
                }
            }
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            // Normal completion or host shutdown.
        } catch (Exception ex) {
            _log.LogError(ex, "Failed to renew search write lease for {Index}", stagingIndex);
            leaseLost.Cancel();
        }
    }

    private void EnsureStagingIndex(string indexName) {
        if (string.Equals(indexName, _settings.IndexName, StringComparison.Ordinal)
            || !indexName.StartsWith(_settings.IndexName + "_", StringComparison.Ordinal)) {
            throw new InvalidOperationException(
                $"Refusing to mutate non-staging search index {indexName}.");
        }
    }

    private static async Task StopHeartbeatAsync(CancellationTokenSource? stop, Task? heartbeat) {
        if (stop == null || heartbeat == null) return;

        stop.Cancel();
        await heartbeat;
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken ct) {
        if (response.IsSuccessStatusCode) return;

        string error = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(
            $"Elasticsearch {operation} failed with {(int)response.StatusCode} ({response.StatusCode}): {error}");
    }

    private static ProductDocument CreateDocument(
        ProductSyncData data,
        List<string>? origNumbers,
        PricingDependencyRevisions pricingRevisions) {
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
            RetailCurrencyCodeVat = data.RetailCurrencyCodeVat ?? data.RetailCurrencyCode ?? "UAH",
            IndexedProductPricingRevision = pricingRevisions.ProductPricing,
            IndexedPricingHierarchyRevision = pricingRevisions.PricingHierarchy,
            IndexedDiscountRevision = pricingRevisions.Discounts,
            IndexedExchangeRateRevision = pricingRevisions.ExchangeRates,
            CatalogOrganizationIdNonVat = data.CatalogOrganizationIdNonVat,
            CatalogOrganizationIdVat = data.CatalogOrganizationIdVat,
            CatalogAgreementSourceNonVat = data.CatalogAgreementSourceNonVat ?? "",
            CatalogAgreementSourceVat = data.CatalogAgreementSourceVat ?? "",
            ProductSourceFenix = data.ProductSourceFenix ?? "",
            ProductSourceAmg = data.ProductSourceAmg ?? "",
            IsCanonicalFenix = data.IsCanonicalFenix,
            IsCanonicalAmg = data.IsCanonicalAmg,
            CatalogScopes = data.CatalogScopes.Select(scope => new ProductCatalogScopeDocument {
                OrganizationId = scope.OrganizationId,
                SourceSystem = scope.SourceSystem,
                WithVat = scope.WithVat,
                AvailableQtyUk = scope.AvailableQtyUk,
                AvailableQtyPl = scope.AvailableQtyPl,
                AvailableQty = scope.AvailableQty
            }).ToList(),
            CatalogSourceSystemNonVat = GetSourceSystem(data.CatalogAgreementSourceNonVat),
            CatalogSourceSystemVat = GetSourceSystem(data.CatalogAgreementSourceVat),
            CatalogAgreementNetUidNonVat = data.CatalogAgreementNetUidNonVat.ToString(),
            CatalogAgreementNetUidVat = data.CatalogAgreementNetUidVat.ToString(),
            CatalogPricingIdNonVat = data.CatalogPricingIdNonVat,
            CatalogPricingIdVat = data.CatalogPricingIdVat,
            CatalogCurrencyIdNonVat = data.CatalogCurrencyIdNonVat,
            CatalogCurrencyIdVat = data.CatalogCurrencyIdVat,
            HasNonVatCatalogAvailability = data.HasNonVatCatalogAvailability,
            HasVatCatalogAvailability = data.HasVatCatalogAvailability,
            HasNonVatCatalogSource = data.HasNonVatCatalogSource,
            HasVatCatalogSource = data.HasVatCatalogSource,
            UpdatedAt = data.Updated
        };
    }

    private static string GetSourceSystem(string? source) {
        if (string.IsNullOrWhiteSpace(source)) return string.Empty;
        int separatorIndex = source.IndexOf(':');
        return separatorIndex > 0 ? source[..separatorIndex].ToLowerInvariant() : string.Empty;
    }

    private enum GenerationSyncMode {
        Full,
        Incremental,
        Targeted
    }

    private enum PromotionResolution {
        PreviousGeneration,
        Promoted,
        Unknown
    }
}
