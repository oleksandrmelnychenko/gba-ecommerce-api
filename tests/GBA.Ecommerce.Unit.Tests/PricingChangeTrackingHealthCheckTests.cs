using System.Data;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Ecommerce;
using GBA.Services.Services.Products;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class PricingChangeTrackingHealthCheckTests {
    [Fact]
    public async Task ExtraTrackedTable_ReportsUnhealthyWithActualCounts() {
        Mock<IDbConnection> connection = new();
        connection.Setup(item => item.Dispose());
        Mock<IDbConnectionFactory> connectionFactory = new(MockBehavior.Strict);
        connectionFactory.Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);
        PricingChangeTrackingStatus status = new(
            PricingDependencyRevisions.Unavailable,
            ExpectedTrackedTableCount: 15,
            ActualTrackedTableCount: 16,
            MissingTrackedTableCount: 0,
            ExtraTrackedTableCount: 1,
            ExpectedPriceFunctionCount: 2,
            ActualPriceFunctionCount: 2,
            UnlistedPriceInputCount: 0,
            NonInputManifestEntryCount: 0,
            ActualPricingModuleCount: 3,
            UnreadablePricingModuleCount: 0,
            RecoveryIncarnationPresent: true,
            RecoveryLineageMatches: true,
            UnreadableTrackedTableIdentityCount: 0,
            UnresolvedPriceDependencyCount: 1,
            CrossDatabasePriceDependencyCount: 0,
            SynonymBackedPriceDependencyCount: 0,
            RepairGeneration: 4,
            RepairFenceValid: true);
        Mock<IPricingDependencyRevisionProvider> provider = new(MockBehavior.Strict);
        provider.Setup(service => service.GetStatus(connection.Object))
            .Returns(status);
        PricingChangeTrackingHealthCheck healthCheck = new(
            connectionFactory.Object,
            provider.Object);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal(15, result.Data["expectedTrackedTableCount"]);
        Assert.Equal(16, result.Data["actualTrackedTableCount"]);
        Assert.Equal(0, result.Data["missingTrackedTableCount"]);
        Assert.Equal(1, result.Data["extraTrackedTableCount"]);
        Assert.Equal(3, result.Data["actualPricingModuleCount"]);
        Assert.Equal(0, result.Data["unreadableTrackedTableIdentityCount"]);
        Assert.Equal(1, result.Data["unresolvedPriceDependencyCount"]);
        Assert.Equal(4L, result.Data["repairGeneration"]);
        Assert.True(Assert.IsType<bool>(result.Data["repairFenceValid"]));
        Assert.True(Assert.IsType<bool>(result.Data["recoveryLineageMatches"]));
        Assert.False(Assert.IsType<bool>(result.Data["recoveryRotationRequired"]));
        connectionFactory.VerifyAll();
        provider.VerifyAll();
    }

    [Fact]
    public async Task StatusReadFailure_ReportsUnhealthy() {
        Mock<IDbConnectionFactory> connectionFactory = new(MockBehavior.Strict);
        connectionFactory.Setup(factory => factory.NewSqlConnection())
            .Throws(new InvalidOperationException("database unavailable"));
        PricingChangeTrackingHealthCheck healthCheck = new(
            connectionFactory.Object,
            Mock.Of<IPricingDependencyRevisionProvider>());

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal(15, result.Data["expectedTrackedTableCount"]);
        connectionFactory.VerifyAll();
    }
}
