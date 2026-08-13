using System;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Sync;

/// <summary>
/// Immutable readiness snapshot for the Elasticsearch product projection.
/// </summary>
public sealed record SearchSyncHealthSnapshot(
    bool Healthy,
    bool IndexExists,
    bool Ready,
    DateTime? LastSyncUtc,
    double? LagSeconds,
    bool Stale,
    string? Error);

/// <summary>
/// Evaluates both Elasticsearch availability and freshness of the last successful
/// product synchronization.
/// </summary>
public sealed class SearchSyncHealthProbe(
    IElasticsearchIndexService indexService,
    ISearchSyncStateStore syncStateStore,
    IOptions<SyncSettings> syncSettings,
    ILogger<SearchSyncHealthProbe> logger) {
    /// <summary>
    /// Reads the cluster state and persisted watermark without changing either one.
    /// </summary>
    /// <param name="ct">Caller cancellation.</param>
    /// <returns>A readiness snapshot suitable for API and host health checks.</returns>
    public async Task<SearchSyncHealthSnapshot> GetSnapshotAsync(
        CancellationToken ct = default) {
        try {
            bool healthy = await indexService.IsHealthyAsync(ct);
            bool indexExists = healthy &&
                               await indexService.IndexExistsAsync(ct);
            DateTime watermark = await syncStateStore.GetWatermarkAsync(ct);
            bool hasWatermark = watermark != DateTime.MinValue;
            double? lagSeconds = hasWatermark
                ? Math.Max(
                    0,
                    Math.Round((DateTime.UtcNow - watermark).TotalSeconds))
                : null;
            int lagLimitSeconds = Math.Max(
                1,
                syncSettings.Value.LagWarningSeconds);
            bool stale = !hasWatermark ||
                         lagSeconds > lagLimitSeconds;
            bool ready = healthy && indexExists && !stale;

            string? error = ready
                ? null
                : !healthy
                    ? "Elasticsearch cluster is unavailable."
                    : !indexExists
                        ? "Elasticsearch product index is unavailable."
                    : !hasWatermark
                        ? "Elasticsearch product sync has no successful watermark."
                        : $"Elasticsearch product sync is stale by " +
                          $"{lagSeconds:0} seconds.";

            return new SearchSyncHealthSnapshot(
                healthy,
                indexExists,
                ready,
                hasWatermark ? watermark : null,
                lagSeconds,
                stale,
                error);
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            throw;
        } catch (Exception exception) {
            logger.LogError(
                exception,
                "Elasticsearch product readiness probe failed");
            return new SearchSyncHealthSnapshot(
                false,
                false,
                false,
                null,
                null,
                true,
                "Elasticsearch product index readiness check failed.");
        }
    }
}
