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

        return snapshot.Ready
            ? HealthCheckResult.Healthy(data: data)
            : HealthCheckResult.Unhealthy(
                snapshot.Error ?? "Elasticsearch product projection is unavailable.",
                data: data);
    }
}
