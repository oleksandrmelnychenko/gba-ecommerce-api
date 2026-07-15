using System.Text.Json;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Ecommerce;
using GBA.Ecommerce.Controllers;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using GBA.Search.Sync;
using GBA.Services.Services.Products;
using GBA.Services.Services.Products.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class ElasticsearchReadinessTests {
    [Fact]
    public async Task HostedReadiness_CurrentCaughtUpGenerationPasses() {
        Mock<IElasticsearchIndexService> index = HealthyIndex();
        Mock<ISearchSyncStateStore> state = StateReturning(ReadyGeneration());
        ElasticsearchReadinessHealthCheck check = CreateHealthCheck(index.Object, state.Object);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.False(Assert.IsType<bool>(result.Data["stale"]));
        Assert.False(Assert.IsType<bool>(result.Data["incrementalCatchUpRequired"]));
        index.VerifyAll();
        state.VerifyAll();
    }

    [Theory]
    [InlineData(ElasticsearchHealthStatus.Degraded)]
    [InlineData(ElasticsearchHealthStatus.Unhealthy)]
    public async Task HostedReadiness_NonReleaseReadyGenerationFails(
        ElasticsearchHealthStatus status) {
        Mock<IElasticsearchIndexService> index = new(MockBehavior.Strict);
        index.Setup(service => service.GetHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Report(status));
        Mock<ISearchSyncStateStore> state = StateReturning(ReadyGeneration());
        ElasticsearchReadinessHealthCheck check = CreateHealthCheck(index.Object, state.Object);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        index.VerifyAll();
        state.VerifyAll();
    }

    [Fact]
    public async Task HostedReadiness_MissingWatermarkFailsClosed() {
        Mock<IElasticsearchIndexService> index = HealthyIndex();
        SearchActiveGeneration generation = ReadyGeneration() with {
            State = ReadyGeneration().State with { WatermarkUtc = DateTime.MinValue }
        };
        Mock<ISearchSyncStateStore> state = StateReturning(generation);
        ElasticsearchReadinessHealthCheck check = CreateHealthCheck(index.Object, state.Object);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.False(Assert.IsType<bool>(result.Data["hasWatermark"]));
        Assert.True(Assert.IsType<bool>(result.Data["stale"]));
    }

    [Fact]
    public async Task HostedReadiness_StaleWatermarkFailsClosed() {
        Mock<IElasticsearchIndexService> index = HealthyIndex();
        Mock<ISearchSyncStateStore> state = StateReturning(
            ReadyGeneration(DateTime.UtcNow.AddMinutes(-10)));
        ElasticsearchReadinessHealthCheck check = CreateHealthCheck(index.Object, state.Object);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.True(Assert.IsType<bool>(result.Data["stale"]));
        Assert.True(Assert.IsType<double>(result.Data["lagSeconds"]) > 300);
    }

    [Fact]
    public async Task HostedReadiness_MismatchedSearchSchemaFailsClosed() {
        Mock<IElasticsearchIndexService> index = HealthyIndex();
        SearchActiveGeneration ready = ReadyGeneration();
        Mock<ISearchSyncStateStore> state = StateReturning(ready with {
            State = ready.State with { SchemaVersion = "legacy-search-schema" }
        });
        ElasticsearchReadinessHealthCheck check = CreateHealthCheck(index.Object, state.Object);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.False(Assert.IsType<bool>(result.Data["schemaCurrent"]));
        Assert.True(Assert.IsType<bool>(result.Data["stale"]));
        Assert.Contains(
            Assert.IsType<string[]>(result.Data["reasons"]),
            reason => reason.Contains("schema", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HostedReadiness_FullRebuildWithoutIncrementalCatchUpFailsClosed() {
        Mock<IElasticsearchIndexService> index = HealthyIndex();
        SearchActiveGeneration ready = ReadyGeneration();
        Mock<ISearchSyncStateStore> state = StateReturning(ready with {
            State = ready.State with { LastIncrementalCatchUpUtc = null }
        });
        ElasticsearchReadinessHealthCheck check = CreateHealthCheck(index.Object, state.Object);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.True(Assert.IsType<bool>(result.Data["incrementalCatchUpRequired"]));
        Assert.True(Assert.IsType<bool>(result.Data["stale"]));
    }

    [Fact]
    public async Task ExplicitHealth_UnhealthyGenerationReturnsServiceUnavailable() {
        Mock<IElasticsearchIndexService> index = new(MockBehavior.Strict);
        index.Setup(service => service.GetHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Report(ElasticsearchHealthStatus.Unhealthy));
        Mock<ISearchSyncStateStore> state = StateReturning(ReadyGeneration());
        ElasticsearchController controller = CreateController(index.Object, state.Object);

        IActionResult action = await controller.HealthAsync(CancellationToken.None);

        AssertServiceUnavailable(action);
        index.VerifyAll();
        state.VerifyAll();
    }

    [Fact]
    public async Task ExplicitHealth_CurrentCaughtUpGenerationReturnsOkAndStaleFalse() {
        Mock<IElasticsearchIndexService> index = HealthyIndex();
        Mock<ISearchSyncStateStore> state = StateReturning(ReadyGeneration());
        ElasticsearchController controller = CreateController(index.Object, state.Object);

        IActionResult action = await controller.HealthAsync(CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        JsonElement body = ReadEnvelopeBody(action);
        Assert.True(body.GetProperty("healthy").GetBoolean());
        Assert.False(body.GetProperty("stale").GetBoolean());
        Assert.False(body.GetProperty("incrementalCatchUpRequired").GetBoolean());
        index.VerifyAll();
        state.VerifyAll();
    }

    [Fact]
    public async Task ExplicitHealth_StaleWatermarkReturnsServiceUnavailable() {
        Mock<IElasticsearchIndexService> index = HealthyIndex();
        Mock<ISearchSyncStateStore> state = StateReturning(
            ReadyGeneration(DateTime.UtcNow.AddMinutes(-10)));
        ElasticsearchController controller = CreateController(index.Object, state.Object);

        IActionResult action = await controller.HealthAsync(CancellationToken.None);

        AssertServiceUnavailable(action);
        Assert.True(ReadEnvelopeBody(action).GetProperty("stale").GetBoolean());
    }

    [Fact]
    public async Task ExplicitHealth_UnreadableSyncStateReturnsServiceUnavailable() {
        Mock<IElasticsearchIndexService> index = HealthyIndex();
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("state unavailable"));
        ElasticsearchController controller = CreateController(index.Object, state.Object);

        IActionResult action = await controller.HealthAsync(CancellationToken.None);

        AssertServiceUnavailable(action);
        JsonElement body = ReadEnvelopeBody(action);
        Assert.False(body.GetProperty("syncStateReadable").GetBoolean());
        Assert.True(body.GetProperty("stale").GetBoolean());
        index.VerifyAll();
        state.VerifyAll();
    }

    [Fact]
    public async Task FullSync_FailedResultReturnsServiceUnavailableWithFailureBody() {
        SyncResult failedResult = SyncResult.Failed("pricing fencing unavailable");
        Mock<IElasticsearchSyncService> sync = new(MockBehavior.Strict);
        sync.Setup(service => service.FullRebuildAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);
        ElasticsearchController controller = new(
            Mock.Of<IElasticsearchIndexService>(),
            sync.Object,
            Mock.Of<IElasticsearchProductSearchService>(),
            Mock.Of<ISearchServingGenerationResolver>(),
            Mock.Of<IProductService>(),
            Mock.Of<IOutputCacheStore>(),
            new ResponseFactory());

        IActionResult action = await controller.FullSyncAsync(CancellationToken.None);

        ObjectResult unavailable = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(unavailable.Value);
        SyncResult responseBody = Assert.IsType<SyncResult>(response.Body);
        Assert.False(responseBody.Success);
        Assert.Equal("pricing fencing unavailable", responseBody.Error);
        sync.VerifyAll();
    }

    private static ElasticsearchReadinessHealthCheck CreateHealthCheck(
        IElasticsearchIndexService index,
        ISearchSyncStateStore state) {
        return new ElasticsearchReadinessHealthCheck(
            index,
            CreateResolver(state));
    }

    private static ElasticsearchController CreateController(
        IElasticsearchIndexService index,
        ISearchSyncStateStore state) {
        return new ElasticsearchController(
            index,
            Mock.Of<IElasticsearchSyncService>(),
            Mock.Of<IElasticsearchProductSearchService>(),
            CreateResolver(state),
            Mock.Of<IProductService>(),
            Mock.Of<IOutputCacheStore>(),
            new ResponseFactory());
    }

    private static ISearchServingGenerationResolver CreateResolver(
        ISearchSyncStateStore state) {
        return new SearchServingGenerationResolver(
            state,
            Options.Create(new SyncSettings { LagWarningSeconds = 300 }),
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            TimeProvider.System);
    }

    private static Mock<IElasticsearchIndexService> HealthyIndex() {
        Mock<IElasticsearchIndexService> index = new(MockBehavior.Strict);
        index.Setup(service => service.GetHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Report(ElasticsearchHealthStatus.Healthy));
        return index;
    }

    private static Mock<ISearchSyncStateStore> StateReturning(SearchActiveGeneration generation) {
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(generation);
        return state;
    }

    private static SearchActiveGeneration ReadyGeneration(DateTime? watermarkUtc = null) {
        DateTime watermark = watermarkUtc ?? DateTime.UtcNow.AddSeconds(-30);
        DateTime rebuildStarted = watermark.AddMinutes(-10);
        DateTime rebuildCompleted = watermark.AddMinutes(-9);
        SearchSyncState state = new(
            watermark,
            EcommercePricingSchema.Version,
            rebuildCompleted,
            "config-v1",
            RetailConfigurationEpoch: 1,
            new PricingDependencyRevisions("p", "h", "d", "e"),
            rebuildStarted,
            LastIncrementalCatchUpUtc: watermark);
        return new SearchActiveGeneration("products_20260715090000000", 1, state, "config-v1", 1);
    }

    private static void AssertServiceUnavailable(IActionResult action) {
        ObjectResult unavailable = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(unavailable.Value);
        Assert.NotNull(response.Body);
    }

    private static JsonElement ReadEnvelopeBody(IActionResult action) {
        ObjectResult result = Assert.IsAssignableFrom<ObjectResult>(action);
        IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(result.Value);
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(response.Body));
        return document.RootElement.Clone();
    }

    private static ElasticsearchHealthReport Report(ElasticsearchHealthStatus status) {
        bool healthy = status == ElasticsearchHealthStatus.Healthy;
        return new ElasticsearchHealthReport(
            status,
            ClusterAvailable: true,
            ClusterStatus: "green",
            HasActiveGeneration: true,
            PointedIndexExists: true,
            ConfigurationConsistent: healthy,
            Reasons: healthy ? [] : ["not release ready"],
            AliasConsistent: healthy,
            PricingRevisionsCurrent: healthy);
    }
}
