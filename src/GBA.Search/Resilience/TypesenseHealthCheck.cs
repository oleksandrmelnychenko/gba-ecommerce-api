using System;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly.CircuitBreaker;

namespace GBA.Search.Resilience;

public sealed class TypesenseHealthCheck(ResilientSearchService searchService) : IHealthCheck {
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {

        try {
            CircuitState circuitState = searchService.GetCircuitState();
            bool isHealthy = await searchService.IsHealthyAsync(cancellationToken);

            if (isHealthy) {
                return HealthCheckResult.Healthy($"Typesense is healthy. Circuit: {circuitState}");
            }

            if (circuitState == Polly.CircuitBreaker.CircuitState.Open) {
                return HealthCheckResult.Degraded(
                    $"Typesense circuit is open (using SQL fallback). Circuit: {circuitState}");
            }

            return HealthCheckResult.Unhealthy($"Typesense is not responding. Circuit: {circuitState}");
        } catch (Exception ex) {
            return HealthCheckResult.Unhealthy("Typesense health check failed", ex);
        }
    }
}
