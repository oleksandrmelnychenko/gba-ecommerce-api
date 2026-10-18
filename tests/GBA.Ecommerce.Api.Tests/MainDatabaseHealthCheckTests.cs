using System.Diagnostics;
using GBA.Ecommerce.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GBA.Ecommerce.Api.Tests;

public sealed class MainDatabaseHealthCheckTests {
    [Fact]
    public async Task Successful_database_probe_reports_healthy() {
        MainDatabaseHealthCheck healthCheck = new(_ => Task.CompletedTask);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task Unresponsive_database_probe_honors_timeout_and_reports_unhealthy() {
        TaskCompletionSource blockedProbe = new(TaskCreationOptions.RunContinuationsAsynchronously);
        MainDatabaseHealthCheck healthCheck = new(_ => blockedProbe.Task);
        using CancellationTokenSource timeout = new(TimeSpan.FromMilliseconds(50));
        Stopwatch stopwatch = Stopwatch.StartNew();

        try {
            HealthCheckResult result = await healthCheck.CheckHealthAsync(
                new HealthCheckContext(),
                timeout.Token);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Equal("Main database connection timed out.", result.Description);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
        } finally {
            blockedProbe.TrySetResult();
        }
    }
}
