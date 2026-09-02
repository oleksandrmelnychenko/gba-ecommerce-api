using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Sync;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GBA.Ecommerce.HealthChecks;

internal sealed class SearchIndexHealthCheck(
    SearchSyncHealthProbe healthProbe) : IHealthCheck {
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        SearchSyncHealthSnapshot snapshot =
            await healthProbe.GetSnapshotAsync(cancellationToken);
        Dictionary<string, object> data = new() {
            ["healthy"] = snapshot.Healthy,
            ["indexExists"] = snapshot.IndexExists,
            ["ready"] = snapshot.Ready,
            ["stale"] = snapshot.Stale,
            ["lastSyncUtc"] = snapshot.LastSyncUtc?.ToString("O") ?? string.Empty,
            ["lagSeconds"] = snapshot.LagSeconds ?? -1
        };

        if (snapshot.Ready) {
            return HealthCheckResult.Healthy(data: data);
        }

        string description = snapshot.Error ??
                             "Elasticsearch product projection is unavailable.";

        // An existing index can continue serving searches while synchronization catches
        // up, even when its freshness is unknown because no watermark is available. Keep
        // that state visible as degraded without reporting a hard dependency outage.
        // A missing index or unavailable cluster remains unhealthy.
        return snapshot is {
            Healthy: true,
            IndexExists: true,
            Stale: true
        }
            ? HealthCheckResult.Degraded(description, data: data)
            : HealthCheckResult.Unhealthy(description, data: data);
    }
}
