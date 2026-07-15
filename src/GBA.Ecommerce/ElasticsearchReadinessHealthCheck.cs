using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Elasticsearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GBA.Ecommerce;

public sealed class ElasticsearchReadinessHealthCheck(
    IElasticsearchIndexService indexService,
    ISearchServingGenerationResolver servingGenerationResolver) : IHealthCheck {
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        try {
            ElasticsearchHealthReport report = await indexService.GetHealthAsync(cancellationToken);
            SearchServingGenerationResolution syncReadiness =
                await servingGenerationResolver.ResolveAsync(cancellationToken);

            IReadOnlyDictionary<string, object> data = new Dictionary<string, object> {
                ["status"] = report.Status.ToString(),
                ["clusterAvailable"] = report.ClusterAvailable,
                ["clusterStatus"] = report.ClusterStatus ?? string.Empty,
                ["hasActiveGeneration"] = report.HasActiveGeneration,
                ["pointedIndexExists"] = report.PointedIndexExists,
                ["configurationConsistent"] = report.ConfigurationConsistent,
                ["pricingRevisionsCurrent"] = report.PricingRevisionsCurrent,
                ["aliasConsistent"] = report.AliasConsistent,
                ["syncStateReadable"] = syncReadiness.SyncStateReadable,
                ["schemaCurrent"] = syncReadiness.SchemaCurrent,
                ["hasWatermark"] = syncReadiness.HasWatermark,
                ["lastSyncUtc"] = syncReadiness.LastSyncUtc?.ToString("O") ?? string.Empty,
                ["lagSeconds"] = syncReadiness.LagSeconds ?? -1,
                ["stale"] = syncReadiness.Stale,
                ["incrementalCatchUpRequired"] = syncReadiness.IncrementalCatchUpRequired,
                ["lastFullRebuildStartedUtc"] =
                    syncReadiness.LastFullRebuildStartedUtc?.ToString("O") ?? string.Empty,
                ["lastIncrementalCatchUpUtc"] =
                    syncReadiness.LastIncrementalCatchUpUtc?.ToString("O") ?? string.Empty,
                ["reasons"] = report.Reasons.Concat(syncReadiness.Reasons).ToArray()
            };

            bool ready = report.Status == ElasticsearchHealthStatus.Healthy
                         && syncReadiness.IsAvailable;
            return ready
                ? HealthCheckResult.Healthy(
                    "Elasticsearch has a current, caught-up active generation.",
                    data)
                : HealthCheckResult.Unhealthy(
                    "Elasticsearch has no release-ready active generation.",
                    data: data);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception exception) {
            return HealthCheckResult.Unhealthy(
                "Elasticsearch readiness could not be verified.",
                exception);
        }
    }
}
