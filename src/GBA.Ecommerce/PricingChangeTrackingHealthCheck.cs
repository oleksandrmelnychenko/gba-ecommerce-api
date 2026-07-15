using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Services.Services.Products;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GBA.Ecommerce;

public sealed class PricingChangeTrackingHealthCheck(
    IDbConnectionFactory connectionFactory,
    IPricingDependencyRevisionProvider revisionProvider) : IHealthCheck {
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        try {
            using IDbConnection connection = connectionFactory.NewSqlConnection();
            PricingChangeTrackingStatus status = revisionProvider.GetStatus(connection);
            HealthCheckResult result = status.IsAvailable
                ? HealthCheckResult.Healthy(
                    "Pricing Change Tracking, recovery incarnation, and module hash are current.",
                    status.ToHealthData())
                : HealthCheckResult.Unhealthy(
                    status.RecoveryIncarnationPresent && !status.RecoveryLineageMatches
                        ? "Pricing recovery-incarnation rotation is required; indexed pricing remains disabled."
                        : "Pricing cache is in database read-through mode because revision fencing is unavailable.",
                    data: status.ToHealthData());
            return Task.FromResult(result);
        } catch (Exception exception) {
            PricingChangeTrackingStatus status = PricingChangeTrackingStatus.QueryFailed(
                SqlPricingDependencyRevisionProvider.ExpectedTrackedTableCount);
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Pricing Change Tracking status could not be read; database read-through mode remains active.",
                exception,
                status.ToHealthData()));
        }
    }
}
