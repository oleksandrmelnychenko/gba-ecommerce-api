using System.Net;
using System.Reflection;
using System.Text;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using GBA.Search.Sync;
using GBA.Services.Services.Products;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class ElasticsearchSyncServiceTests {
    private const string ActiveIndex = "products_20260714030000000";
    private const string StagingIndex = "products_20260714040000000";
    private const string ConfigurationSignature = "config-v1";
    private static readonly PricingDependencyRevisions PricingRevisions = new(
        "product-pricing:db:1",
        "pricing-hierarchy:db:1",
        "discounts:db:1",
        "exchange-rates:db:1");

    [Fact]
    public void ProductDocumentMapper_PreservesExactProductAndAgreementSources() {
        ProductSyncData data = Product(42);
        data.CatalogAgreementSourceNonVat = "amg:id-A1|code-11";
        data.CatalogAgreementSourceVat = "fenix:id-B2|code-12";
        data.ProductSourceFenix = "fenix:id-CAFE|code-99";
        data.ProductSourceAmg = "amg:id-BEEF|code-77";
        data.CatalogScopes = [
            new ProductCatalogScopeData {
                OrganizationId = 17,
                SourceSystem = "amg",
                WithVat = false,
                AvailableQtyUk = 7,
                AvailableQtyPl = 3,
                AvailableQty = 10
            }
        ];
        MethodInfo mapper = typeof(ElasticsearchSyncService).GetMethod(
            "CreateDocument",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Product document mapper was not found.");

        ProductDocument document = Assert.IsType<ProductDocument>(
            mapper.Invoke(null, [data, null, PricingRevisions]));

        Assert.Equal(data.CatalogAgreementSourceNonVat, document.CatalogAgreementSourceNonVat);
        Assert.Equal(data.CatalogAgreementSourceVat, document.CatalogAgreementSourceVat);
        Assert.Equal(data.ProductSourceFenix, document.ProductSourceFenix);
        Assert.Equal(data.ProductSourceAmg, document.ProductSourceAmg);
        Assert.Equal("amg", document.CatalogSourceSystemNonVat);
        Assert.Equal("fenix", document.CatalogSourceSystemVat);
        Assert.Equal(PricingRevisions.ProductPricing, document.IndexedProductPricingRevision);
        Assert.Equal(PricingRevisions.PricingHierarchy, document.IndexedPricingHierarchyRevision);
        Assert.Equal(PricingRevisions.Discounts, document.IndexedDiscountRevision);
        Assert.Equal(PricingRevisions.ExchangeRates, document.IndexedExchangeRateRevision);
        ProductCatalogScopeDocument scope = Assert.Single(document.CatalogScopes);
        Assert.Equal(17, scope.OrganizationId);
        Assert.Equal("amg", scope.SourceSystem);
        Assert.Equal(10, scope.AvailableQty);
    }

    [Fact]
    public async Task Full_MultiItemPartialBulkFailure_NeverPromotesOrWritesLiveIndex() {
        SyncFixture fixture = CreateFixture(GenerationMode.Full, batchSize: 2);
        fixture.Repository.Setup(repository => repository.GetProductProjectionBatchAsync(
                0,
                2,
                ConfigurationSignature))
            .ReturnsAsync(new ProductProjectionBatch(
                [Product(41), Product(42)],
                ConfigurationSignature,
                true,
                42,
                2,
                false));
        fixture.Handler.BulkResponses.Enqueue(
            "{\"errors\":true,\"items\":["
            + "{\"index\":{\"status\":201}},"
            + "{\"index\":{\"status\":429,\"error\":{\"type\":\"rejected\"}}}]}"
        );

        SyncResult result = await fixture.Service.FullRebuildAsync();

        Assert.False(result.Success);
        Assert.Contains("failed for 1 of 2", result.Error, StringComparison.OrdinalIgnoreCase);
        string bulk = Assert.Single(fixture.Handler.BulkBodies);
        Assert.Contains($"\"_index\":\"{StagingIndex}\"", bulk, StringComparison.Ordinal);
        Assert.DoesNotContain("\"_index\":\"products\"", bulk, StringComparison.Ordinal);
        fixture.State.Verify(store => store.PromoteGenerationAsync(
            It.IsAny<SearchRebuildLease>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<PricingDependencyRevisions>(),
            It.IsAny<CancellationToken>()), Times.Never);
        fixture.Index.Verify(service => service.DeleteFailedVersionedIndexAsync(
            fixture.Lease,
            StagingIndex,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Full_StreamsBoundedKeysetBatches_ThenPromotesExactGeneration() {
        SyncFixture fixture = CreateFixture(GenerationMode.Full, batchSize: 5000);
        fixture.Repository.Setup(repository => repository.GetProductProjectionBatchAsync(
                0,
                2000,
                ConfigurationSignature))
            .ReturnsAsync(new ProductProjectionBatch(
                [Product(42)],
                ConfigurationSignature,
                true,
                2000,
                2000,
                true));
        fixture.Repository.Setup(repository => repository.GetProductProjectionBatchAsync(
                2000,
                2000,
                ConfigurationSignature))
            .ReturnsAsync(new ProductProjectionBatch(
                [Product(2042)],
                ConfigurationSignature,
                true,
                2042,
                42,
                false));
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index"));
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index"));

        SyncResult result = await fixture.Service.FullRebuildAsync();

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, result.DocumentsIndexed);
        fixture.Repository.Verify(repository => repository.GetProductProjectionBatchAsync(
            It.IsAny<long>(),
            It.Is<int>(take => take <= 2000),
            ConfigurationSignature), Times.Exactly(2));
        fixture.State.Verify(store => store.PromoteGenerationAsync(
            fixture.Lease,
            StagingIndex,
            It.IsAny<DateTime>(),
            EcommercePricingSchema.Version,
            It.Is<DateTime?>(value => value.HasValue),
            ConfigurationSignature,
            ConfigurationSignature,
            It.Is<PricingDependencyRevisions>(revisions =>
                revisions.MatchesExactly(PricingRevisions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Incremental_ClonesActiveAndMutatesOnlyStagingBeforePromotion() {
        SyncFixture fixture = CreateFixture(GenerationMode.Incremental, batchSize: 100);
        fixture.Repository.Setup(repository => repository.GetChangedProductIdBatchAsync(
                It.IsAny<DateTime>(),
                ConfigurationSignature,
                0,
                100))
            .ReturnsAsync(new ProductIdSyncBatch(
                [42], ConfigurationSignature, false, true, 42, false));
        fixture.Repository.Setup(repository => repository.GetProductProjectionByIdsAsync(
                It.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new long[] { 42L })),
                ConfigurationSignature))
            .ReturnsAsync(new ProductProjectionSnapshot([Product(42)], ConfigurationSignature, true));
        fixture.Repository.Setup(repository => repository.GetDeletedProductIdBatchAsync(
                It.IsAny<DateTime>(),
                0,
                100))
            .ReturnsAsync([99]);
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index"));
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("delete"));

        SyncResult result = await fixture.Service.IncrementalSyncAsync();

        Assert.True(result.Success, result.Error);
        fixture.Index.Verify(service => service.CloneGenerationAsync(
            fixture.Lease,
            ActiveIndex,
            StagingIndex,
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.All(fixture.Handler.BulkBodies, body => {
            Assert.Contains($"\"_index\":\"{StagingIndex}\"", body, StringComparison.Ordinal);
            Assert.DoesNotContain("\"_index\":\"products\"", body, StringComparison.Ordinal);
            Assert.DoesNotContain($"\"_index\":\"{ActiveIndex}\"", body, StringComparison.Ordinal);
        });
        fixture.State.Verify(store => store.PromoteGenerationAsync(
            fixture.Lease,
            StagingIndex,
            It.IsAny<DateTime>(),
            EcommercePricingSchema.Version,
            It.Is<DateTime?>(value => !value.HasValue),
            ConfigurationSignature,
            ConfigurationSignature,
            It.Is<PricingDependencyRevisions>(revisions =>
                revisions.MatchesExactly(PricingRevisions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Incremental_WithRotatedPricingRevision_RequiresFullGeneration() {
        SyncFixture fixture = CreateFixture(GenerationMode.Incremental, batchSize: 100);
        PricingDependencyRevisions oldIncarnation = PricingRevisions with {
            ProductPricing = "product-pricing:old-incarnation"
        };
        SearchSyncState staleState = new(
            DateTime.UtcNow.AddMinutes(-5),
            EcommercePricingSchema.Version,
            DateTime.UtcNow.AddHours(-1),
            ConfigurationSignature,
            RetailConfigurationEpoch: 3,
            IndexedPricingRevisions: oldIncarnation);
        fixture.State.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchActiveGeneration(
                ActiveIndex,
                Generation: 3,
                staleState,
                ConfigurationSignature,
                ObservedConfigurationEpoch: 3));
        fixture.Repository.Setup(repository => repository.GetProductProjectionBatchAsync(
                0,
                100,
                ConfigurationSignature))
            .ReturnsAsync(new ProductProjectionBatch(
                [Product(42)],
                ConfigurationSignature,
                true,
                42,
                42,
                false));
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index"));

        SyncResult result = await fixture.Service.IncrementalSyncAsync();

        Assert.True(result.Success, result.Error);
        fixture.Repository.Verify(repository => repository.GetProductProjectionBatchAsync(
            0,
            100,
            ConfigurationSignature), Times.Once);
        fixture.Repository.Verify(repository => repository.GetChangedProductIdBatchAsync(
            It.IsAny<DateTime>(),
            It.IsAny<string?>(),
            It.IsAny<long>(),
            It.IsAny<int>()), Times.Never);
        fixture.Index.Verify(service => service.CloneGenerationAsync(
            It.IsAny<SearchRebuildLease>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Targeted_ConfigDriftAfterClone_LeavesPriorGenerationUntouched() {
        SyncFixture fixture = CreateFixture(GenerationMode.Targeted, batchSize: 100);
        fixture.Repository.Setup(repository => repository.GetProductProjectionByIdsAsync(
                It.IsAny<IReadOnlyCollection<long>>(),
                ConfigurationSignature))
            .ReturnsAsync(new ProductProjectionSnapshot([], "config-v2", false));

        SyncResult result = await fixture.Service.ReindexProductsAsync([42]);

        Assert.False(result.Success);
        Assert.Empty(fixture.Handler.BulkBodies);
        fixture.State.Verify(store => store.PromoteGenerationAsync(
            It.IsAny<SearchRebuildLease>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<PricingDependencyRevisions>(),
            It.IsAny<CancellationToken>()), Times.Never);
        fixture.Index.Verify(service => service.DeleteFailedVersionedIndexAsync(
            fixture.Lease,
            StagingIndex,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfigurationMutationAfterStagingWrite_NeverPromotesMixedGeneration() {
        SyncFixture fixture = CreateFixture(GenerationMode.Full, batchSize: 100);
        fixture.Repository.Setup(repository => repository.GetProductProjectionBatchAsync(
                0,
                100,
                ConfigurationSignature))
            .ReturnsAsync(new ProductProjectionBatch(
                [Product(42)],
                ConfigurationSignature,
                true,
                42,
                1,
                false));
        fixture.Repository.Setup(repository => repository.IsRetailConfigurationCurrentAsync(
                ConfigurationSignature))
            .ReturnsAsync(false);
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index"));

        SyncResult result = await fixture.Service.FullRebuildAsync();

        Assert.False(result.Success);
        Assert.Contains("configuration changed", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Single(fixture.Handler.BulkBodies);
        Assert.Contains($"\"_index\":\"{StagingIndex}\"", fixture.Handler.BulkBodies[0], StringComparison.Ordinal);
        fixture.State.Verify(store => store.PromoteGenerationAsync(
            It.IsAny<SearchRebuildLease>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<PricingDependencyRevisions>(),
            It.IsAny<CancellationToken>()), Times.Never);
        fixture.Index.Verify(service => service.DeleteFailedVersionedIndexAsync(
            fixture.Lease,
            StagingIndex,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PricingChangeTrackingAdvanceAfterBuild_NeverPromotesStaleIndexedPrices() {
        PricingDependencyRevisions advancedRevisions = PricingRevisions with {
            ProductPricing = "product-pricing:db:2"
        };
        SyncFixture fixture = CreateFixture(GenerationMode.Full, batchSize: 100);
        fixture.Repository.SetupSequence(repository =>
                repository.GetPricingDependencyRevisionsAsync())
            .ReturnsAsync(PricingRevisions)
            .ReturnsAsync(advancedRevisions);
        fixture.Repository.Setup(repository => repository.GetProductProjectionBatchAsync(
                0,
                100,
                ConfigurationSignature))
            .ReturnsAsync(new ProductProjectionBatch(
                [Product(42)],
                ConfigurationSignature,
                true,
                42,
                1,
                false));
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index"));

        SyncResult result = await fixture.Service.FullRebuildAsync();

        Assert.False(result.Success);
        Assert.Contains("Change Tracking advanced", result.Error, StringComparison.OrdinalIgnoreCase);
        fixture.State.Verify(store => store.PromoteGenerationAsync(
            It.IsAny<SearchRebuildLease>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<PricingDependencyRevisions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AliasMode_ConfigurationDriftAfterCutover_RestoresDurableAliasTarget() {
        SyncFixture fixture = CreateFixture(
            GenerationMode.Full,
            batchSize: 100,
            useAliasSwap: true);
        fixture.Repository.SetupSequence(repository =>
                repository.IsRetailConfigurationCurrentAsync(ConfigurationSignature))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        fixture.Repository.Setup(repository => repository.GetProductProjectionBatchAsync(
                0,
                100,
                ConfigurationSignature))
            .ReturnsAsync(new ProductProjectionBatch(
                [Product(42)],
                ConfigurationSignature,
                true,
                42,
                1,
                false));
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index"));

        SyncResult result = await fixture.Service.FullRebuildAsync();

        Assert.False(result.Success);
        Assert.Contains("configuration changed", result.Error, StringComparison.OrdinalIgnoreCase);
        fixture.Index.Verify(service => service.SwapAliasAsync(
            fixture.Lease,
            StagingIndex,
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.Index.Verify(service => service.RestoreAliasAsync(
            fixture.Lease,
            StagingIndex,
            CancellationToken.None), Times.Once);
        fixture.State.Verify(store => store.PromoteGenerationAsync(
            It.IsAny<SearchRebuildLease>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<PricingDependencyRevisions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AliasMode_PromotionCasRejected_RestoresDurableAliasTarget() {
        SyncFixture fixture = CreateFixture(
            GenerationMode.Full,
            batchSize: 100,
            useAliasSwap: true);
        fixture.Repository.Setup(repository => repository.GetProductProjectionBatchAsync(
                0,
                100,
                ConfigurationSignature))
            .ReturnsAsync(new ProductProjectionBatch(
                [Product(42)],
                ConfigurationSignature,
                true,
                42,
                1,
                false));
        fixture.State.Setup(store => store.PromoteGenerationAsync(
                fixture.Lease,
                StagingIndex,
                It.IsAny<DateTime>(),
                EcommercePricingSchema.Version,
                It.IsAny<DateTime?>(),
                ConfigurationSignature,
                ConfigurationSignature,
                It.IsAny<PricingDependencyRevisions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((SearchGenerationAcknowledgement?)null);
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index"));

        SyncResult result = await fixture.Service.FullRebuildAsync();

        Assert.False(result.Success);
        Assert.Contains("not acknowledged", result.Error, StringComparison.OrdinalIgnoreCase);
        fixture.Index.Verify(service => service.RestoreAliasAsync(
            fixture.Lease,
            StagingIndex,
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task AliasMode_LostPromotionResponseWithCommittedDurableState_DoesNotRollBack() {
        SyncFixture fixture = CreateFixture(
            GenerationMode.Full,
            batchSize: 100,
            useAliasSwap: true);
        fixture.Repository.Setup(repository => repository.GetProductProjectionBatchAsync(
                0,
                100,
                ConfigurationSignature))
            .ReturnsAsync(new ProductProjectionBatch(
                [Product(42)],
                ConfigurationSignature,
                true,
                42,
                1,
                false));
        SearchSyncState committedState = new(
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            DateTime.UtcNow,
            ConfigurationSignature,
            RetailConfigurationEpoch: fixture.Lease.ConfigurationEpoch,
            IndexedPricingRevisions: PricingRevisions);
        SearchActiveGeneration committed = new(
            StagingIndex,
            fixture.Lease.ExpectedGeneration + 1,
            committedState,
            ConfigurationSignature,
            fixture.Lease.ConfigurationEpoch);
        fixture.State.SetupSequence(store =>
                store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchActiveGeneration(
                ActiveIndex,
                fixture.Lease.ExpectedGeneration,
                committedState,
                ConfigurationSignature,
                fixture.Lease.ConfigurationEpoch))
            .ReturnsAsync(committed);
        fixture.State.Setup(store => store.PromoteGenerationAsync(
                fixture.Lease,
                StagingIndex,
                It.IsAny<DateTime>(),
                EcommercePricingSchema.Version,
                It.IsAny<DateTime?>(),
                ConfigurationSignature,
                ConfigurationSignature,
                It.IsAny<PricingDependencyRevisions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("response lost"));
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index"));

        SyncResult result = await fixture.Service.FullRebuildAsync();

        Assert.True(result.Success, result.Error);
        fixture.Index.Verify(service => service.RestoreAliasAsync(
            It.IsAny<SearchRebuildLease>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(GenerationMode.Full)]
    [InlineData(GenerationMode.Incremental)]
    [InlineData(GenerationMode.Targeted)]
    public async Task EveryMode_UsesSameDistributedLease_AndFailsBeforeWritesWhenDenied(
        GenerationMode mode) {
        SyncFixture fixture = CreateFixture(mode, leaseGranted: false);

        SyncResult result = mode switch {
            GenerationMode.Full => await fixture.Service.FullRebuildAsync(),
            GenerationMode.Incremental => await fixture.Service.IncrementalSyncAsync(),
            GenerationMode.Targeted => await fixture.Service.ReindexProductsAsync([42]),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        Assert.False(result.Success);
        Assert.Contains("shared search write lease", result.Error, StringComparison.OrdinalIgnoreCase);
        fixture.State.Verify(store => store.TryAcquireWriteLeaseAsync(
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.Index.VerifyNoOtherCalls();
        Assert.Empty(fixture.Handler.Requests);
    }

    [Fact]
    public async Task Targeted_ProcessesLargeInputInBoundedBatches() {
        SyncFixture fixture = CreateFixture(GenerationMode.Targeted, batchSize: 2000);
        long[] ids = Enumerable.Range(1, 4501).Select(value => (long)value).ToArray();
        fixture.Repository.Setup(repository => repository.GetProductProjectionByIdsAsync(
                It.Is<IReadOnlyCollection<long>>(batch => batch.Count <= 2000),
                ConfigurationSignature))
            .ReturnsAsync((IReadOnlyCollection<long> batch, string? _) =>
                new ProductProjectionSnapshot(
                    batch.Select(Product).ToList(),
                    ConfigurationSignature,
                    true));
        fixture.Repository.Setup(repository => repository.GetOriginalNumbersForProductsAsync(
                It.Is<IEnumerable<long>>(batch => batch.Count() <= 2000)))
            .ReturnsAsync([]);
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index", 2000));
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index", 2000));
        fixture.Handler.BulkResponses.Enqueue(BulkSuccess("index", 501));

        SyncResult result = await fixture.Service.ReindexProductsAsync(ids);

        Assert.True(result.Success, result.Error);
        Assert.Equal(4501, result.DocumentsIndexed);
        fixture.Repository.Verify(repository => repository.GetProductProjectionByIdsAsync(
            It.IsAny<IReadOnlyCollection<long>>(),
            ConfigurationSignature), Times.Exactly(3));
        Assert.Equal(3, fixture.Handler.BulkBodies.Count);
    }

    private static SyncFixture CreateFixture(
        GenerationMode mode,
        int batchSize = 100,
        bool leaseGranted = true,
        bool useAliasSwap = false) {
        Mock<IProductSyncRepository> repository = new(MockBehavior.Strict);
        Mock<IElasticsearchIndexService> index = new(MockBehavior.Strict);
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        GenerationHttpHandler handler = new();
        DateTime now = DateTime.UtcNow;
        SearchSyncState currentState = new(
            now.AddMinutes(-5),
            EcommercePricingSchema.Version,
            now.AddHours(-1),
            ConfigurationSignature,
            RetailConfigurationEpoch: 3,
            IndexedPricingRevisions: PricingRevisions,
            LastFullRebuildStartedUtc: now.AddMinutes(-65),
            LastIncrementalCatchUpUtc: now.AddMinutes(-5));
        SearchActiveGeneration active = new(
            ActiveIndex,
            3,
            currentState,
            ConfigurationSignature,
            ObservedConfigurationEpoch: 3);
        SearchRebuildLease acquiredLease = new("owner-a", 7, ActiveIndex, 3);
        SearchRebuildLease lease = acquiredLease with {
            ConfigurationSignature = ConfigurationSignature,
            ConfigurationEpoch = 3
        };

        state.Setup(store => store.TryAcquireWriteLeaseAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaseGranted ? acquiredLease : null);
        if (leaseGranted) {
            state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(active);
            state.Setup(store => store.BindWriteLeaseConfigurationAsync(
                    acquiredLease,
                    ConfigurationSignature,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(lease);
            state.Setup(store => store.RenewWriteLeaseAsync(
                    lease,
                    StagingIndex,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            state.Setup(store => store.ValidateWriteLeaseAsync(
                    lease,
                    StagingIndex,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            state.Setup(store => store.ReleaseWriteLeaseAsync(
                    lease,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            state.Setup(store => store.PromoteGenerationAsync(
                    lease,
                    StagingIndex,
                    It.IsAny<DateTime>(),
                    EcommercePricingSchema.Version,
                    It.IsAny<DateTime?>(),
                    ConfigurationSignature,
                    ConfigurationSignature,
                    It.Is<PricingDependencyRevisions>(revisions =>
                        revisions.MatchesExactly(PricingRevisions)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchGenerationAcknowledgement("owner-a", 7, StagingIndex, 4));

            repository.Setup(repository => repository.GetRetailConfigurationSnapshotAsync())
                .ReturnsAsync(new RetailConfigurationSnapshot {
                    IsValid = true,
                    Signature = ConfigurationSignature
                });
            repository.Setup(repository => repository.GetPricingDependencyRevisionsAsync())
                .ReturnsAsync(PricingRevisions);
            repository.Setup(repository => repository.IsRetailConfigurationCurrentAsync(
                    ConfigurationSignature))
                .ReturnsAsync(true);
            repository.Setup(repository => repository.GetOriginalNumbersForProductsAsync(
                    It.IsAny<IEnumerable<long>>()))
                .ReturnsAsync([]);

            index.Setup(service => service.ValidateConfiguredNameModeAsync(
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            index.Setup(service => service.CreateVersionedIndexAsync(
                    lease,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(StagingIndex);
            index.Setup(service => service.DeleteFailedVersionedIndexAsync(
                    lease,
                    StagingIndex,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            index.Setup(service => service.RefreshGenerationAsync(
                    lease,
                    StagingIndex,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            if (useAliasSwap) {
                index.Setup(service => service.SwapAliasAsync(
                        lease,
                        StagingIndex,
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
                index.Setup(service => service.RestoreAliasAsync(
                        lease,
                        StagingIndex,
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
            }
            if (mode != GenerationMode.Full) {
                index.Setup(service => service.CloneGenerationAsync(
                        lease,
                        ActiveIndex,
                        StagingIndex,
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
            }
        }

        ElasticsearchSyncService service = new(
            new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch/") },
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            Options.Create(new SyncSettings {
                BatchSize = batchSize,
                CleanupOldCollections = false,
                UseAliasSwap = useAliasSwap
            }),
            repository.Object,
            index.Object,
            state.Object,
            NullLogger<ElasticsearchSyncService>.Instance);
        return new SyncFixture(service, repository, index, state, handler, lease);
    }

    private static ProductSyncData Product(long id) => new() {
        Id = id,
        NetUid = Guid.NewGuid(),
        VendorCode = $"SKU-{id}",
        IsForWeb = true,
        RetailPrice = 10,
        RetailPriceVat = 12,
        CatalogAgreementSourceNonVat = "fenix:id-AA|code-1",
        CatalogAgreementSourceVat = "fenix:id-BB|code-2",
        ProductSourceFenix = "fenix:id-AA|code-1",
        CatalogAgreementNetUidNonVat = Guid.NewGuid(),
        CatalogAgreementNetUidVat = Guid.NewGuid(),
        CatalogPricingIdNonVat = 1,
        CatalogPricingIdVat = 2,
        CatalogCurrencyIdNonVat = 1,
        CatalogCurrencyIdVat = 1,
        HasNonVatCatalogAvailability = true,
        HasNonVatCatalogSource = true
    };

    private static string BulkSuccess(string action, int count = 1) {
        string item = $"{{\"{action}\":{{\"status\":201}}}}";
        return $"{{\"errors\":false,\"items\":[{string.Join(',', Enumerable.Repeat(item, count))}]}}";
    }

    public enum GenerationMode {
        Full,
        Incremental,
        Targeted
    }

    private sealed record SyncFixture(
        ElasticsearchSyncService Service,
        Mock<IProductSyncRepository> Repository,
        Mock<IElasticsearchIndexService> Index,
        Mock<ISearchSyncStateStore> State,
        GenerationHttpHandler Handler,
        SearchRebuildLease Lease);

    private sealed class GenerationHttpHandler : HttpMessageHandler {
        public Queue<string> BulkResponses { get; } = [];
        public List<string> BulkBodies { get; } = [];
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            Requests.Add(request);
            if (request.RequestUri!.AbsolutePath == "/_bulk") {
                BulkBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
                return JsonResponse(BulkResponses.Dequeue());
            }

            if (request.RequestUri.AbsolutePath == $"/{StagingIndex}/_refresh") {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            throw new InvalidOperationException(
                $"Unexpected Elasticsearch request: {request.Method} {request.RequestUri}");
        }
    }

    private static HttpResponseMessage JsonResponse(string body) => new(HttpStatusCode.OK) {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
}
