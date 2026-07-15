using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GBA.Services.Infrastructure.SalesMutations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GBA.Ecommerce;

public sealed class SalesMutationOutboxHealthCheck : IHealthCheck {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SalesMutationOutboxOptions _options;
    private readonly SalesMutationInternalAuthOptions _internalAuthOptions;
    private readonly TimeProvider _timeProvider;

    public SalesMutationOutboxHealthCheck(
        IServiceScopeFactory scopeFactory,
        SalesMutationOutboxOptions options,
        SalesMutationInternalAuthOptions internalAuthOptions,
        TimeProvider timeProvider) {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _internalAuthOptions = internalAuthOptions ??
            throw new ArgumentNullException(nameof(internalAuthOptions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _options.Validate();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        try {
            _internalAuthOptions.GetValidatedApiKey();
        } catch (InvalidOperationException exception) {
            return HealthCheckResult.Unhealthy(
                "Internal authentication for durable CRM sales mutations is not configured.",
                exception);
        }

        try {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISalesMutationOutboxStore store =
                scope.ServiceProvider.GetRequiredService<ISalesMutationOutboxStore>();
            SalesMutationOutboxStats stats = await store.GetStatsAsync(cancellationToken);
            Dictionary<string, object> data = new() {
                ["pending"] = stats.PendingCount,
                ["leased"] = stats.LeasedCount,
                ["deadLetters"] = stats.DeadLetterCount,
                ["authenticationFailures"] = stats.AuthenticationFailureCount,
                ["oldestPendingUtc"] = stats.OldestPendingUtc?.ToString("O") ?? string.Empty
            };

            if (stats.AuthenticationFailureCount > 0)
                return HealthCheckResult.Unhealthy(
                    $"The sales mutation outbox contains {stats.AuthenticationFailureCount} message(s) with an authentication delivery failure.",
                    data: data);

            if (stats.DeadLetterCount > 0)
                return HealthCheckResult.Unhealthy(
                    $"The sales mutation outbox contains {stats.DeadLetterCount} dead-letter message(s).",
                    data: data);

            DateTime unhealthyBefore = _timeProvider.GetUtcNow().UtcDateTime
                .Subtract(_options.PendingUnhealthyAfter);
            if (stats.OldestPendingUtc.HasValue && stats.OldestPendingUtc.Value < unhealthyBefore)
                return HealthCheckResult.Unhealthy(
                    "The sales mutation outbox contains an overdue pending message.",
                    data: data);

            return HealthCheckResult.Healthy("The durable sales mutation outbox is operational.", data);
        } catch (Exception exception) {
            return HealthCheckResult.Unhealthy(
                "Unable to query the durable sales mutation outbox.",
                exception);
        }
    }
}
