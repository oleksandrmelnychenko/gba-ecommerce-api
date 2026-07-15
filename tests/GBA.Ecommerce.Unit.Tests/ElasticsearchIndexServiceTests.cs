using System.Net;
using System.Text;
using System.Text.Json;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using GBA.Search.Sync;
using GBA.Services.Services.Products;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class ElasticsearchIndexServiceTests {
    private static readonly PricingDependencyRevisions PricingRevisions = new(
        "product-pricing:revision-v1",
        "pricing-hierarchy:revision-v1",
        "discounts:revision-v1",
        "exchange-rates:revision-v1");

    private static readonly SearchRebuildLease Lease = new(
        "coordinator-a",
        FencingToken: 7,
        ConfigurationSignature: "config-v1",
        ConfigurationEpoch: 3);

    [Fact]
    public async Task CreateVersionedIndex_MapsCatalogIdentityAndRetailPricesForExactFiltering() {
        StubHttpMessageHandler handler = new(request => request.Method == HttpMethod.Put
            && request.RequestUri!.AbsolutePath.StartsWith("/products_", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}"));
        ElasticsearchIndexService service = CreateService(handler);

        string? createdIndex = await service.CreateVersionedIndexAsync(Lease);

        Assert.NotNull(createdIndex);
        Assert.StartsWith("products_", createdIndex, StringComparison.Ordinal);
        HttpRequestMessage request = Assert.Single(handler.Requests);
        using JsonDocument document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        JsonElement properties = document.RootElement
            .GetProperty("mappings")
            .GetProperty("properties");
        Assert.Equal("long", properties.GetProperty("catalogOrganizationIdNonVat").GetProperty("type").GetString());
        Assert.Equal("long", properties.GetProperty("catalogOrganizationIdVat").GetProperty("type").GetString());
        Assert.Equal(
            "keyword",
            properties.GetProperty("catalogAgreementSourceNonVat").GetProperty("fields").GetProperty("keyword").GetProperty("type").GetString());
        Assert.Equal(
            "keyword",
            properties.GetProperty("catalogAgreementSourceVat").GetProperty("fields").GetProperty("keyword").GetProperty("type").GetString());
        Assert.Equal("keyword", properties.GetProperty("productSourceFenix").GetProperty("type").GetString());
        Assert.Equal("keyword", properties.GetProperty("productSourceAmg").GetProperty("type").GetString());
        JsonElement catalogScopes = properties.GetProperty("catalogScopes");
        Assert.Equal("nested", catalogScopes.GetProperty("type").GetString());
        Assert.Equal(
            "long",
            catalogScopes.GetProperty("properties").GetProperty("organizationId").GetProperty("type").GetString());
        Assert.Equal(
            "keyword",
            catalogScopes.GetProperty("properties").GetProperty("sourceSystem").GetProperty("type").GetString());
        Assert.Equal("long", properties.GetProperty("catalogPricingIdNonVat").GetProperty("type").GetString());
        Assert.Equal("long", properties.GetProperty("catalogPricingIdVat").GetProperty("type").GetString());
        Assert.Equal("boolean", properties.GetProperty("hasNonVatCatalogAvailability").GetProperty("type").GetString());
        Assert.Equal("boolean", properties.GetProperty("hasVatCatalogAvailability").GetProperty("type").GetString());
        Assert.Equal("scaled_float", properties.GetProperty("retailPrice").GetProperty("type").GetString());
        Assert.Equal("scaled_float", properties.GetProperty("retailPriceVat").GetProperty("type").GetString());
    }

    [Fact]
    public async Task DirectCreateDeleteAndEnsure_AreDisabledBeforeAnyHttpMutation() {
        StubHttpMessageHandler handler = new(request =>
            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}"));
        ElasticsearchIndexService service = CreateService(handler);

        InvalidOperationException create = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateIndexAsync());
        InvalidOperationException delete = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteIndexAsync());
        InvalidOperationException ensure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EnsureConcreteIndexAsync());

        Assert.Contains("fenced generation", create.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fenced generation", delete.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fenced generation", ensure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task EveryGenerationMutation_RejectsStaleFenceBeforeHttp() {
        StubHttpMessageHandler handler = new(request =>
            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}"));
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.ValidateWriteLeaseAsync(
                Lease,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        ElasticsearchIndexService service = CreateService(
            handler,
            state.Object,
            Mock.Of<IProductSyncRepository>());

        string? created = await service.CreateVersionedIndexAsync(Lease);
        bool cloned = await service.CloneGenerationAsync(
            Lease,
            "products_20260714010000000",
            "products_20260714020000000");
        bool refreshed = await service.RefreshGenerationAsync(
            Lease,
            "products_20260714020000000");
        bool swapped = await service.SwapAliasAsync(Lease, "products_20260714020000000");
        bool restored = await service.RestoreAliasAsync(Lease, "products_20260714020000000");
        bool deleted = await service.DeleteFailedVersionedIndexAsync(
            Lease,
            "products_20260714020000000");
        int cleaned = await service.CleanupOldVersionedIndicesAsync(Lease, keep: 2);

        Assert.Null(created);
        Assert.False(cloned);
        Assert.False(refreshed);
        Assert.False(swapped);
        Assert.False(restored);
        Assert.False(deleted);
        Assert.Equal(0, cleaned);
        Assert.Empty(handler.Requests);
        state.Verify(store => store.ValidateWriteLeaseAsync(
            Lease,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Exactly(7));
    }

    [Fact]
    public async Task Health_IsHealthyOnlyForExistingGenerationWithCurrentConfiguration() {
        const string activeIndex = "products_20260714010000000";
        StubHttpMessageHandler handler = new(request => request switch {
            { Method: var method } when method == HttpMethod.Get
                                      && request.RequestUri!.AbsolutePath == "/_cluster/health" =>
                JsonResponse("{\"status\":\"green\"}"),
            { Method: var method } when method == HttpMethod.Head
                                      && request.RequestUri!.AbsolutePath == $"/{activeIndex}" =>
                new HttpResponseMessage(HttpStatusCode.OK),
            _ => throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}")
        });
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveGeneration(activeIndex, "config-v1", epoch: 4));
        Mock<IProductSyncRepository> repository = new(MockBehavior.Strict);
        repository.Setup(repo => repo.GetRetailConfigurationSnapshotAsync())
            .ReturnsAsync(new RetailConfigurationSnapshot { IsValid = true, Signature = "config-v1" });
        repository.Setup(repo => repo.GetPricingDependencyRevisionsAsync())
            .ReturnsAsync(PricingRevisions);
        ElasticsearchIndexService service = CreateService(handler, state.Object, repository.Object);

        ElasticsearchHealthReport report = await service.GetHealthAsync();

        Assert.Equal(ElasticsearchHealthStatus.Healthy, report.Status);
        Assert.True(report.HasActiveGeneration);
        Assert.True(report.PointedIndexExists);
        Assert.True(report.ConfigurationConsistent);
        Assert.True(report.PricingRevisionsCurrent);
    }

    [Fact]
    public async Task Health_MissingActiveGenerationIsUnhealthy() {
        StubHttpMessageHandler handler = new(request =>
            request.RequestUri!.AbsolutePath == "/_cluster/health"
                ? JsonResponse("{\"status\":\"yellow\"}")
                : throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}"));
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SearchActiveGeneration?)null);
        ElasticsearchIndexService service = CreateService(
            handler,
            state.Object,
            Mock.Of<IProductSyncRepository>());

        ElasticsearchHealthReport report = await service.GetHealthAsync();

        Assert.Equal(ElasticsearchHealthStatus.Unhealthy, report.Status);
        Assert.False(report.HasActiveGeneration);
        Assert.False(report.PointedIndexExists);
    }

    [Fact]
    public async Task Health_MissingPointedIndexIsUnhealthy() {
        const string activeIndex = "products_20260714010000000";
        StubHttpMessageHandler handler = new(request => request.Method == HttpMethod.Get
                                                        && request.RequestUri!.AbsolutePath == "/_cluster/health"
            ? JsonResponse("{\"status\":\"green\"}")
            : request.Method == HttpMethod.Head
              && request.RequestUri!.AbsolutePath == $"/{activeIndex}"
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}"));
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveGeneration(activeIndex, "config-v1", epoch: 4));
        ElasticsearchIndexService service = CreateService(
            handler,
            state.Object,
            Mock.Of<IProductSyncRepository>());

        ElasticsearchHealthReport report = await service.GetHealthAsync();

        Assert.Equal(ElasticsearchHealthStatus.Unhealthy, report.Status);
        Assert.True(report.HasActiveGeneration);
        Assert.False(report.PointedIndexExists);
    }

    [Fact]
    public async Task Health_StaleSqlConfigurationIsDegraded() {
        const string activeIndex = "products_20260714010000000";
        StubHttpMessageHandler handler = new(request => request.Method == HttpMethod.Get
                                                        && request.RequestUri!.AbsolutePath == "/_cluster/health"
            ? JsonResponse("{\"status\":\"green\"}")
            : request.Method == HttpMethod.Head
              && request.RequestUri!.AbsolutePath == $"/{activeIndex}"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}"));
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveGeneration(activeIndex, "config-v1", epoch: 4));
        Mock<IProductSyncRepository> repository = new(MockBehavior.Strict);
        repository.Setup(repo => repo.GetRetailConfigurationSnapshotAsync())
            .ReturnsAsync(new RetailConfigurationSnapshot { IsValid = true, Signature = "config-v2" });
        ElasticsearchIndexService service = CreateService(handler, state.Object, repository.Object);

        ElasticsearchHealthReport report = await service.GetHealthAsync();

        Assert.Equal(ElasticsearchHealthStatus.Degraded, report.Status);
        Assert.True(report.PointedIndexExists);
        Assert.False(report.ConfigurationConsistent);
    }

    [Fact]
    public async Task Health_DurableConfigurationEpochMismatchIsDegraded() {
        const string activeIndex = "products_20260714010000000";
        StubHttpMessageHandler handler = new(request => request.Method == HttpMethod.Get
                                                        && request.RequestUri!.AbsolutePath == "/_cluster/health"
            ? JsonResponse("{\"status\":\"green\"}")
            : request.Method == HttpMethod.Head
              && request.RequestUri!.AbsolutePath == $"/{activeIndex}"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}"));
        SearchSyncState activeState = new(
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            DateTime.UtcNow,
            "config-v1",
            RetailConfigurationEpoch: 3);
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchActiveGeneration(
                activeIndex,
                Generation: 3,
                activeState,
                ObservedConfigurationSignature: "config-v1",
                ObservedConfigurationEpoch: 4));
        Mock<IProductSyncRepository> repository = new(MockBehavior.Strict);
        repository.Setup(repo => repo.GetRetailConfigurationSnapshotAsync())
            .ReturnsAsync(new RetailConfigurationSnapshot { IsValid = true, Signature = "config-v1" });
        ElasticsearchIndexService service = CreateService(handler, state.Object, repository.Object);

        ElasticsearchHealthReport report = await service.GetHealthAsync();

        Assert.Equal(ElasticsearchHealthStatus.Degraded, report.Status);
        Assert.True(report.PointedIndexExists);
        Assert.False(report.ConfigurationConsistent);
    }

    [Fact]
    public async Task Health_RotatedPricingRevisionWithoutPromotionIsDegraded() {
        const string activeIndex = "products_20260714010000000";
        PricingDependencyRevisions rotatedRevisions = PricingRevisions with {
            ProductPricing = "product-pricing:revision-v2"
        };
        StubHttpMessageHandler handler = new(request => request.Method == HttpMethod.Get
                                                        && request.RequestUri!.AbsolutePath == "/_cluster/health"
            ? JsonResponse("{\"status\":\"green\"}")
            : request.Method == HttpMethod.Head
              && request.RequestUri!.AbsolutePath == $"/{activeIndex}"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}"));
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveGeneration(activeIndex, "config-v1", epoch: 4));
        Mock<IProductSyncRepository> repository = new(MockBehavior.Strict);
        repository.Setup(repo => repo.GetRetailConfigurationSnapshotAsync())
            .ReturnsAsync(new RetailConfigurationSnapshot { IsValid = true, Signature = "config-v1" });
        repository.Setup(repo => repo.GetPricingDependencyRevisionsAsync())
            .ReturnsAsync(rotatedRevisions);
        ElasticsearchIndexService service = CreateService(handler, state.Object, repository.Object);

        ElasticsearchHealthReport report = await service.GetHealthAsync();

        Assert.Equal(ElasticsearchHealthStatus.Degraded, report.Status);
        Assert.True(report.ConfigurationConsistent);
        Assert.False(report.PricingRevisionsCurrent);
        Assert.Contains(
            report.Reasons,
            reason => reason.Contains("pricing revision", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Health_AliasMismatchWithDurableGenerationIsUnhealthy() {
        const string activeIndex = "products_20260714010000000";
        const string staleAliasTarget = "products_20260714000000000";
        StubHttpMessageHandler handler = new(request => request switch {
            { Method: var method } when method == HttpMethod.Get
                                      && request.RequestUri!.AbsolutePath == "/_cluster/health" =>
                JsonResponse("{\"status\":\"green\"}"),
            { Method: var method } when method == HttpMethod.Head
                                      && request.RequestUri!.AbsolutePath == $"/{activeIndex}" =>
                new HttpResponseMessage(HttpStatusCode.OK),
            { Method: var method } when method == HttpMethod.Get
                                      && request.RequestUri!.AbsolutePath == "/_alias/products" =>
                JsonResponse($"{{\"{staleAliasTarget}\":{{\"aliases\":{{\"products\":{{}}}}}}}}"),
            _ => throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}")
        });
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveGeneration(activeIndex, "config-v1", epoch: 4));
        Mock<IProductSyncRepository> repository = new(MockBehavior.Strict);
        repository.Setup(repo => repo.GetRetailConfigurationSnapshotAsync())
            .ReturnsAsync(new RetailConfigurationSnapshot { IsValid = true, Signature = "config-v1" });
        repository.Setup(repo => repo.GetPricingDependencyRevisionsAsync())
            .ReturnsAsync(PricingRevisions);
        ElasticsearchIndexService service = CreateService(
            handler,
            state.Object,
            repository.Object,
            useAliasSwap: true);

        ElasticsearchHealthReport report = await service.GetHealthAsync();

        Assert.Equal(ElasticsearchHealthStatus.Unhealthy, report.Status);
        Assert.False(report.AliasConsistent);
        Assert.Contains(report.Reasons, reason => reason.Contains("alias", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Cleanup_ProtectsAliasTargetAndActiveRebuildIndexByIdentity() {
        const string aliasTarget = "products_20260714010000000";
        const string activeRebuildIndex = "products_20260714020000000";
        const string lexicallyNewestUnprotected = "products_20260714040000000";
        DateTime leaseExpiration = DateTime.UtcNow.AddMinutes(2);

        StubHttpMessageHandler handler = new(request => request.RequestUri!.AbsolutePath switch {
            "/_alias/products" => JsonResponse($"{{\"{aliasTarget}\":{{\"aliases\":{{\"products\":{{}}}}}}}}"),
            "/products_sync_state/_doc/generation-control" => JsonResponse(
                $"{{\"_source\":{{\"activeIndex\":\"{aliasTarget}\",\"activeGeneration\":3,\"leaseExpiresAtUtc\":\"{leaseExpiration:O}\",\"stagingIndex\":\"{activeRebuildIndex}\"}}}}"),
            "/_cat/indices/products_*" => JsonResponse(
                $"[{{\"index\":\"{aliasTarget}\"}},{{\"index\":\"{activeRebuildIndex}\"}},{{\"index\":\"{lexicallyNewestUnprotected}\"}},{{\"index\":\"products_sync_state\"}}]"),
            _ when request.Method == HttpMethod.Delete => new HttpResponseMessage(HttpStatusCode.OK),
            _ => throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}")
        });
        ElasticsearchIndexService service = CreateService(handler);

        int deleted = await service.CleanupOldVersionedIndicesAsync(Lease, keep: 1);

        Assert.Equal(1, deleted);
        HttpRequestMessage delete = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Delete);
        Assert.Equal($"/{lexicallyNewestUnprotected}", delete.RequestUri!.AbsolutePath);
        Assert.DoesNotContain(handler.Requests, request => request.RequestUri!.AbsolutePath == "/products_sync_state");
    }

    [Fact]
    public async Task Cleanup_RetainsNewestUnprotectedBackupAfterProtectedAliasTarget() {
        const string aliasTarget = "products_20260714010000000";
        const string olderBackup = "products_20260714020000000";
        const string newestBackup = "products_20260714030000000";

        StubHttpMessageHandler handler = new(request => request.RequestUri!.AbsolutePath switch {
            "/_alias/products" => JsonResponse($"{{\"{aliasTarget}\":{{\"aliases\":{{\"products\":{{}}}}}}}}"),
            "/products_sync_state/_doc/generation-control" => JsonResponse(
                $"{{\"_source\":{{\"activeIndex\":\"{aliasTarget}\",\"activeGeneration\":3}}}}"),
            "/_cat/indices/products_*" => JsonResponse(
                $"[{{\"index\":\"{aliasTarget}\"}},{{\"index\":\"{olderBackup}\"}},{{\"index\":\"{newestBackup}\"}}]"),
            _ when request.Method == HttpMethod.Delete => new HttpResponseMessage(HttpStatusCode.OK),
            _ => throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}")
        });
        ElasticsearchIndexService service = CreateService(handler);

        int deleted = await service.CleanupOldVersionedIndicesAsync(Lease, keep: 2);

        Assert.Equal(1, deleted);
        HttpRequestMessage delete = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Delete);
        Assert.Equal($"/{olderBackup}", delete.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Cleanup_AbortsWhenAliasTargetsCannotBeRead() {
        StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) {
            Content = new StringContent("unavailable", Encoding.UTF8, "text/plain")
        });
        ElasticsearchIndexService service = CreateService(handler);

        int deleted = await service.CleanupOldVersionedIndicesAsync(Lease, keep: 1);

        Assert.Equal(0, deleted);
        Assert.Single(handler.Requests);
        Assert.DoesNotContain(handler.Requests, request => request.Method == HttpMethod.Delete);
    }

    [Fact]
    public async Task Cleanup_LostFenceImmediatelyBeforeDeleteCannotDeleteReplacementStaging() {
        const string aliasTarget = "products_20260714010000000";
        const string replacementStaging = "products_20260714020000000";
        DateTime leaseExpiration = DateTime.UtcNow.AddMinutes(2);
        StubHttpMessageHandler handler = new(request => request.RequestUri!.AbsolutePath switch {
            "/_alias/products" => JsonResponse($"{{\"{aliasTarget}\":{{\"aliases\":{{\"products\":{{}}}}}}}}"),
            "/products_sync_state/_doc/generation-control" => JsonResponse(
                $"{{\"_source\":{{\"activeIndex\":\"{aliasTarget}\",\"activeGeneration\":3,\"leaseExpiresAtUtc\":\"{leaseExpiration:O}\"}}}}"),
            "/_cat/indices/products_*" => JsonResponse(
                $"[{{\"index\":\"{aliasTarget}\"}},{{\"index\":\"{replacementStaging}\"}}]"),
            _ when request.Method == HttpMethod.Delete => throw new InvalidOperationException(
                "A stale cleanup coordinator must not issue any destructive request."),
            _ => throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}")
        });
        int validations = 0;
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.ValidateWriteLeaseAsync(
                Lease,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Interlocked.Increment(ref validations) < 4);
        ElasticsearchIndexService service = CreateService(
            handler,
            state.Object,
            Mock.Of<IProductSyncRepository>());

        int deleted = await service.CleanupOldVersionedIndicesAsync(Lease, keep: 1);

        Assert.Equal(0, deleted);
        Assert.Equal(4, validations);
        Assert.DoesNotContain(handler.Requests, request => request.Method == HttpMethod.Delete);
    }

    [Fact]
    public async Task SwapAlias_DelayedStaleOwnerCannotSwapBackAfterNewOwnerCutover() {
        const string oldIndex = "products_20260714010000000";
        const string staleIndex = "products_20260714020000000";
        const string currentOwnerIndex = "products_20260714030000000";
        AliasRaceHandler handler = new(oldIndex, staleIndex);
        ElasticsearchIndexService service = CreateService(handler);

        Task<bool> staleSwap = service.SwapAliasAsync(Lease, staleIndex);
        await handler.StaleRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        bool currentOwnerSwap = await service.SwapAliasAsync(Lease, currentOwnerIndex);
        handler.AllowStaleRequest.TrySetResult(true);
        bool staleOwnerSwap = await staleSwap;

        Assert.True(currentOwnerSwap);
        Assert.False(staleOwnerSwap);
        Assert.Equal(currentOwnerIndex, handler.CurrentAliasTarget);
    }

    [Fact]
    public async Task SwapAlias_LegacyConcreteIndexExists_RefusesNonAtomicMigrationWithoutMutation() {
        const string targetIndex = "products_20260714030000000";
        StubHttpMessageHandler handler = new(request => request.RequestUri!.AbsolutePath switch {
            "/_alias/products" => new HttpResponseMessage(HttpStatusCode.NotFound),
            "/products" => JsonResponse("{\"products\":{\"aliases\":{}}}"),
            _ => throw new InvalidOperationException(
                $"Concrete-index migration must not mutate Elasticsearch: {request.Method} {request.RequestUri}")
        });
        ElasticsearchIndexService service = CreateService(handler);

        bool swapped = await service.SwapAliasAsync(Lease, targetIndex);

        Assert.False(swapped);
        Assert.Collection(
            handler.Requests,
            request => Assert.Equal("/_alias/products", request.RequestUri!.AbsolutePath),
            request => Assert.Equal("/products", request.RequestUri!.AbsolutePath));
        Assert.DoesNotContain(handler.Requests, request => request.Method == HttpMethod.Delete);
        Assert.DoesNotContain(handler.Requests, request => request.RequestUri!.AbsolutePath == "/_aliases");
    }

    [Fact]
    public async Task RestoreAlias_AtomicallyReturnsFailedTargetToDurableGeneration() {
        const string priorIndex = "products_20260714010000000";
        const string failedTarget = "products_20260714020000000";
        SearchRebuildLease rollbackLease = Lease with {
            ExpectedActiveIndex = priorIndex,
            ExpectedGeneration = 3
        };
        string currentTarget = failedTarget;
        StubHttpMessageHandler handler = new(request => {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/_alias/products") {
                return JsonResponse($"{{\"{currentTarget}\":{{\"aliases\":{{\"products\":{{}}}}}}}}");
            }

            if (request.Method == HttpMethod.Post
                && request.RequestUri!.AbsolutePath == "/_aliases") {
                string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                using JsonDocument document = JsonDocument.Parse(body);
                JsonElement[] actions = document.RootElement.GetProperty("actions").EnumerateArray().ToArray();
                JsonElement remove = Assert.Single(actions, action => action.TryGetProperty("remove", out _))
                    .GetProperty("remove");
                JsonElement add = Assert.Single(actions, action => action.TryGetProperty("add", out _))
                    .GetProperty("add");
                Assert.Equal(failedTarget, remove.GetProperty("index").GetString());
                Assert.True(remove.GetProperty("must_exist").GetBoolean());
                Assert.Equal(priorIndex, add.GetProperty("index").GetString());
                currentTarget = priorIndex;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        ElasticsearchIndexService service = CreateService(handler);

        bool restored = await service.RestoreAliasAsync(rollbackLease, failedTarget);

        Assert.True(restored);
        Assert.Equal(priorIndex, currentTarget);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task RestoreAlias_FirstGenerationFailureRemovesNewAlias() {
        const string failedTarget = "products_20260714020000000";
        SearchRebuildLease firstGenerationLease = Lease with {
            ExpectedActiveIndex = null,
            ExpectedGeneration = 0
        };
        string? currentTarget = failedTarget;
        StubHttpMessageHandler handler = new(request => {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/_alias/products") {
                return currentTarget == null
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    : JsonResponse($"{{\"{currentTarget}\":{{\"aliases\":{{\"products\":{{}}}}}}}}");
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/products") {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (request.Method == HttpMethod.Post
                && request.RequestUri!.AbsolutePath == "/_aliases") {
                string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                using JsonDocument document = JsonDocument.Parse(body);
                JsonElement action = Assert.Single(
                    document.RootElement.GetProperty("actions").EnumerateArray());
                JsonElement remove = action.GetProperty("remove");
                Assert.Equal(failedTarget, remove.GetProperty("index").GetString());
                Assert.True(remove.GetProperty("must_exist").GetBoolean());
                currentTarget = null;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        ElasticsearchIndexService service = CreateService(handler);

        bool restored = await service.RestoreAliasAsync(firstGenerationLease, failedTarget);

        Assert.True(restored);
        Assert.Null(currentTarget);
        Assert.Equal(4, handler.Requests.Count);
    }

    [Fact]
    public async Task ConcreteMode_RejectsExistingAliasWithoutMutation() {
        StubHttpMessageHandler handler = new(request => request.RequestUri!.AbsolutePath switch {
            "/_alias/products" => JsonResponse("{\"products_v1\":{\"aliases\":{\"products\":{}}}}"),
            _ => throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}")
        });
        ElasticsearchIndexService service = CreateService(handler);

        bool valid = await service.ValidateConfiguredNameModeAsync(useAliasSwap: false);

        Assert.False(valid);
        Assert.Single(handler.Requests);
        Assert.DoesNotContain(
            handler.Requests,
            request => request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete);
    }

    [Fact]
    public async Task AliasMode_RejectsExistingConcreteIndexWithoutMutation() {
        StubHttpMessageHandler handler = new(request => request.RequestUri!.AbsolutePath switch {
            "/_alias/products" => new HttpResponseMessage(HttpStatusCode.NotFound),
            "/products" => JsonResponse("{\"products\":{}}"),
            _ => throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}")
        });
        ElasticsearchIndexService service = CreateService(handler);

        bool valid = await service.ValidateConfiguredNameModeAsync(useAliasSwap: true);

        Assert.False(valid);
        Assert.Equal(2, handler.Requests.Count);
        Assert.DoesNotContain(
            handler.Requests,
            request => request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete);
    }

    [Fact]
    public async Task CloneGeneration_ItemFailureFailsClosed() {
        const string source = "products_20260714010000000";
        const string target = "products_20260714020000000";
        string? requestBody = null;
        StubHttpMessageHandler handler = new(request => {
            if (request.RequestUri!.AbsolutePath != "/_reindex") {
                throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
            }

            requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse("{\"total\":2,\"created\":1,\"updated\":0,\"failures\":[{\"status\":429}]}");
        });
        ElasticsearchIndexService service = CreateService(handler);

        bool cloned = await service.CloneGenerationAsync(Lease, source, target);

        Assert.False(cloned);
        Assert.Single(handler.Requests);
        using JsonDocument body = JsonDocument.Parse(requestBody!);
        Assert.Equal(source, body.RootElement.GetProperty("source").GetProperty("index").GetString());
        Assert.Equal(target, body.RootElement.GetProperty("dest").GetProperty("index").GetString());
        Assert.Equal("create", body.RootElement.GetProperty("dest").GetProperty("op_type").GetString());
    }

    [Fact]
    public async Task DeleteFailedIndex_DeletesOnlyRequestedUnprotectedVersionedIndex() {
        const string aliasTarget = "products_20260714010000000";
        const string failedIndex = "products_20260714020000000";
        const string activeIndex = "products_20260714030000000";
        DateTime leaseExpiration = DateTime.UtcNow.AddMinutes(2);
        StubHttpMessageHandler handler = new(request => request.RequestUri!.AbsolutePath switch {
            "/_alias/products" => JsonResponse($"{{\"{aliasTarget}\":{{\"aliases\":{{\"products\":{{}}}}}}}}"),
            "/products_sync_state/_doc/generation-control" => JsonResponse(
                $"{{\"_source\":{{\"activeIndex\":\"{activeIndex}\",\"activeGeneration\":3,\"leaseExpiresAtUtc\":\"{leaseExpiration:O}\"}}}}"),
            $"/{failedIndex}" when request.Method == HttpMethod.Delete => new HttpResponseMessage(HttpStatusCode.OK),
            _ => throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}")
        });
        ElasticsearchIndexService service = CreateService(handler);

        bool deleted = await service.DeleteFailedVersionedIndexAsync(Lease, failedIndex);

        Assert.True(deleted);
        HttpRequestMessage delete = Assert.Single(handler.Requests, request => request.Method == HttpMethod.Delete);
        Assert.Equal($"/{failedIndex}", delete.RequestUri!.AbsolutePath);
        Assert.DoesNotContain(handler.Requests, request => request.RequestUri!.AbsolutePath == "/_cat/indices/products_*");
    }

    [Fact]
    public async Task DeleteFailedIndex_ProtectsCurrentAliasTarget() {
        const string failedIndex = "products_20260714020000000";
        StubHttpMessageHandler handler = new(request => request.RequestUri!.AbsolutePath switch {
            "/_alias/products" => JsonResponse($"{{\"{failedIndex}\":{{\"aliases\":{{\"products\":{{}}}}}}}}"),
            _ => throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}")
        });
        ElasticsearchIndexService service = CreateService(handler);

        bool deleted = await service.DeleteFailedVersionedIndexAsync(Lease, failedIndex);

        Assert.False(deleted);
        Assert.DoesNotContain(handler.Requests, request => request.Method == HttpMethod.Delete);
    }

    [Fact]
    public async Task DeleteFailedIndex_DeletesExactOwnedStagingBeforeLeaseRelease() {
        const string aliasTarget = "products_20260714010000000";
        const string failedIndex = "products_20260714020000000";
        DateTime leaseExpiration = DateTime.UtcNow.AddMinutes(2);
        StubHttpMessageHandler handler = new(request => request.RequestUri!.AbsolutePath switch {
            "/_alias/products" => JsonResponse($"{{\"{aliasTarget}\":{{\"aliases\":{{\"products\":{{}}}}}}}}"),
            "/products_sync_state/_doc/generation-control" => JsonResponse(
                $"{{\"_source\":{{\"activeIndex\":\"{aliasTarget}\",\"activeGeneration\":3,\"leaseExpiresAtUtc\":\"{leaseExpiration:O}\",\"stagingIndex\":\"{failedIndex}\"}}}}"),
            $"/{failedIndex}" when request.Method == HttpMethod.Delete =>
                new HttpResponseMessage(HttpStatusCode.OK),
            _ => throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}")
        });
        ElasticsearchIndexService service = CreateService(handler);

        bool deleted = await service.DeleteFailedVersionedIndexAsync(Lease, failedIndex);

        Assert.True(deleted);
        Assert.Contains(handler.Requests, request => request.Method == HttpMethod.Delete);
    }

    [Theory]
    [InlineData("products")]
    [InlineData("products_sync_state")]
    [InlineData("products_*")]
    [InlineData("other_20260714020000000")]
    public async Task DeleteFailedIndex_RejectsNonVersionedOrStateIndexNames(string indexName) {
        StubHttpMessageHandler handler = new(request =>
            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}"));
        ElasticsearchIndexService service = CreateService(handler);

        bool deleted = await service.DeleteFailedVersionedIndexAsync(Lease, indexName);

        Assert.False(deleted);
        Assert.Empty(handler.Requests);
    }

    private static SearchActiveGeneration ActiveGeneration(
        string indexName,
        string configurationSignature,
        long epoch) {
        SearchSyncState syncState = new(
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            DateTime.UtcNow,
            configurationSignature,
            epoch,
            PricingRevisions);
        return new SearchActiveGeneration(
            indexName,
            Generation: 3,
            syncState,
            configurationSignature,
            epoch);
    }

    private static ElasticsearchIndexService CreateService(
        HttpMessageHandler handler,
        bool useAliasSwap = false) {
        Mock<ISearchSyncStateStore> state = new();
        state.Setup(store => store.ValidateWriteLeaseAsync(
                It.IsAny<SearchRebuildLease>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IProductSyncRepository> repository = new();
        return CreateService(handler, state.Object, repository.Object, useAliasSwap);
    }

    private static ElasticsearchIndexService CreateService(
        HttpMessageHandler handler,
        ISearchSyncStateStore state,
        IProductSyncRepository repository,
        bool useAliasSwap = false) {
        HttpClient client = new(handler) { BaseAddress = new Uri("http://elasticsearch/") };
        return new ElasticsearchIndexService(
            client,
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            Options.Create(new SyncSettings { UseAliasSwap = useAliasSwap }),
            state,
            repository,
            NullLogger<ElasticsearchIndexService>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string body) {
        return new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private sealed class AliasRaceHandler(
        string initialAliasTarget,
        string delayedTarget) : HttpMessageHandler {
        private readonly object _sync = new();
        private string _currentAliasTarget = initialAliasTarget;

        public TaskCompletionSource<bool> StaleRequestStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> AllowStaleRequest { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string CurrentAliasTarget {
            get {
                lock (_sync) return _currentAliasTarget;
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/_alias/products") {
                string target = CurrentAliasTarget;
                return JsonResponse($"{{\"{target}\":{{\"aliases\":{{\"products\":{{}}}}}}}}");
            }

            if (request.Method != HttpMethod.Post
                || request.RequestUri!.AbsolutePath != "/_aliases") {
                throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
            }

            string body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using JsonDocument document = JsonDocument.Parse(body);
            string? expectedTarget = null;
            string? nextTarget = null;
            foreach (JsonElement action in document.RootElement.GetProperty("actions").EnumerateArray()) {
                if (action.TryGetProperty("remove", out JsonElement remove)) {
                    expectedTarget = remove.GetProperty("index").GetString();
                }
                if (action.TryGetProperty("add", out JsonElement add)) {
                    nextTarget = add.GetProperty("index").GetString();
                }
            }

            if (string.Equals(nextTarget, delayedTarget, StringComparison.Ordinal)) {
                StaleRequestStarted.TrySetResult(true);
                await AllowStaleRequest.Task.WaitAsync(cancellationToken);
            }

            lock (_sync) {
                if (!string.Equals(expectedTarget, _currentAliasTarget, StringComparison.Ordinal)) {
                    return new HttpResponseMessage(HttpStatusCode.Conflict) {
                        Content = new StringContent("alias target changed", Encoding.UTF8, "text/plain")
                    };
                }

                _currentAliasTarget = nextTarget
                    ?? throw new InvalidOperationException("Alias request did not contain an add action.");
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            Requests.Add(request);
            return Task.FromResult(responseFactory(request));
        }
    }
}
