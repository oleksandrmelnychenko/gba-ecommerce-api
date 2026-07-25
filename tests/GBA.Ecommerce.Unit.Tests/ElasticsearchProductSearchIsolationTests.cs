using System.Net;
using System.Text;
using System.Text.Json;
using GBA.Common.Middleware;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Products;
using GBA.Ecommerce.Controllers;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using GBA.Search.Models;
using GBA.Search.Sync;
using GBA.Search.Text;
using GBA.Services.Services.Products;
using GBA.Services.Services.Products.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class ElasticsearchProductSearchIsolationTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Search_FiltersStableWebCatalogIdentityBeforePaging(bool withVat) {
        ProductSearchCatalogContext context = CreateCatalogContext(withVat, useIndexedRetailPrice: false);
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(context));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResultWithDocs result = await service.SearchWithDocsAsync(
            "oil filter",
            context,
            "uk",
            limit: 7,
            offset: 14);

        ProductSearchDocument product = Assert.Single(result.Documents);
        Assert.False(product.Available);

        using JsonDocument request = JsonDocument.Parse(Assert.IsType<string>(handler.RequestBody));
        JsonElement root = request.RootElement;
        Assert.Equal(14, root.GetProperty("from").GetInt32());
        Assert.Equal(7, root.GetProperty("size").GetInt32());
        JsonElement filters = root
            .GetProperty("query")
            .GetProperty("function_score")
            .GetProperty("query")
            .GetProperty("bool")
            .GetProperty("filter");
        string filterJson = filters.GetRawText();
        Assert.Contains("\"isForWeb\":true", filterJson, StringComparison.Ordinal);
        Assert.Contains("\"productSourceFenix\":\"fenix:\"", filterJson, StringComparison.Ordinal);
        Assert.Contains("\"isCanonicalFenix\":true", filterJson, StringComparison.Ordinal);
        Assert.DoesNotContain("catalogOrganizationId", filterJson, StringComparison.Ordinal);
        Assert.DoesNotContain("hasNonVatCatalogAvailability", filterJson, StringComparison.Ordinal);
        Assert.DoesNotContain("hasVatCatalogAvailability", filterJson, StringComparison.Ordinal);
        Assert.DoesNotContain("retailPrice", filterJson, StringComparison.Ordinal);
        Assert.DoesNotContain("indexedProductPricingRevision", filterJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_AmgContextNeverAcceptsFenixDocument() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: false,
            source: "amg");
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(
            context,
            documentSource: "fenix"));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResultWithDocs result = await service.SearchWithDocsAsync("filter", context);

        Assert.Empty(result.Documents);
        Assert.DoesNotContain("\"catalogScopes.", handler.RequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("fenix:", handler.RequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_LiveHydrationModeDoesNotTrustIndexedAvailability() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: false,
            source: "amg");
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(context));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResultWithDocs result = await service.SearchWithDocsAsync("filter", context);

        ProductSearchDocument product = Assert.Single(result.Documents);
        Assert.False(product.Available);
        Assert.Equal(0, product.AvailableQty);
        Assert.Equal(0, product.AvailableQtyUk);
        Assert.Equal(0, product.AvailableQtyPl);
        Assert.Contains("\"productSourceAmg\":\"amg:\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("\"catalogScopes.", handler.RequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("\"available", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"retailPrice", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"indexedPricing", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"catalogOrganization", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_NormalizedEmptyQueryUsesStableCatalogSortWithoutCommercialFields() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: false);
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(context));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        await service.SearchWithDocsAsync("---", context);

        using JsonDocument request = JsonDocument.Parse(Assert.IsType<string>(handler.RequestBody));
        string requestJson = request.RootElement.GetRawText();
        Assert.True(request.RootElement.GetProperty("query").TryGetProperty("bool", out _));
        Assert.Contains("\"nameUA.keyword\"", requestJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"available", requestJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"retailPrice", requestJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"catalogScopes", requestJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"indexedPricing", requestJson, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("fenix", "amg:id-BB|code-22")]
    [InlineData("amg", "fenix:id-AA|code-11")]
    public async Task Search_DualSourceProductIsEligibleForExactRequestedSource(
        string requestedSource,
        string secondarySource) {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: true,
            useIndexedRetailPrice: false,
            source: requestedSource);
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(
            context,
            secondaryProductSource: secondarySource));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResultWithDocs result = await service.SearchWithDocsAsync("filter", context);

        Assert.Single(result.Documents);
        string expectedProductField = requestedSource == "amg"
            ? "productSourceAmg"
            : "productSourceFenix";
        Assert.Contains(
            $"\"{expectedProductField}\":\"{requestedSource}:\"",
            handler.RequestBody,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_NonCanonicalSourceDuplicate_IsRejectedBeforePagingAndCounting() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: false,
            source: "fenix");
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(
            context,
            documentProductSource: "fenix:id-02|code-11",
            documentIsCanonical: false));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResultWithDocs result = await service.SearchWithDocsAsync(
            "filter",
            context,
            limit: 20,
            offset: 0);

        Assert.Empty(result.Documents);
        Assert.Equal(0, result.TotalCount);
        Assert.Contains(
            "\"isCanonicalFenix\":true",
            handler.RequestBody,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_RequestsAndReturnsExactTotalsBeyondTenThousand() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: false);
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(
            context,
            total: 25_001,
            totalRelation: "eq"));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResult result = await service.SearchAsync("filter", context, limit: 20, offset: 0);

        Assert.Equal(25_001, result.TotalCount);
        using JsonDocument request = JsonDocument.Parse(Assert.IsType<string>(handler.RequestBody));
        Assert.True(request.RootElement.GetProperty("track_total_hits").GetBoolean());
    }

    [Fact]
    public async Task Search_NonExactTotalRelation_FailsClosed() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: false);
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(
            context,
            total: 10_000,
            totalRelation: "gte"));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResult result = await service.SearchAsync("filter", context, limit: 20, offset: 0);

        Assert.Empty(result.ProductIds);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Search_LiveHydrationModeDoesNotFilterByIndexedOrganization() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: false);
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(
            context,
            documentOrganizationId: context.OrganizationId + 1));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResultWithDocs result = await service.SearchWithDocsAsync("filter", context);

        Assert.Single(result.Documents);
        Assert.DoesNotContain("\"catalogScopes.", handler.RequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_LiveHydrationModeDoesNotUseIndexedPriceAsEligibility() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: true,
            useIndexedRetailPrice: false);
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(context, retailPrice: 0m));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResultWithDocs result = await service.SearchWithDocsAsync("filter", context);

        Assert.Single(result.Documents);
        Assert.DoesNotContain(
            "\"range\":{\"retailPriceVat\"",
            handler.RequestBody,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_LiveHydrationModeDoesNotFilterByIndexedAgreement() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: false);
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(
            context,
            documentAgreementNetUid: Guid.NewGuid()));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResultWithDocs result = await service.SearchWithDocsAsync("filter", context);

        Assert.Single(result.Documents);
        Assert.DoesNotContain(context.ClientAgreementNetId.ToString(), handler.RequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ContextFreeSearchMethodsFailClosedWithoutCallingElasticsearch() {
        CapturingHttpMessageHandler handler = new(
            throwOnRequest: true,
            responseJson: "");
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResult idResult = await service.SearchAsync("filter");
        ProductSearchResultWithDocs documentResult = await service.SearchWithDocsAsync("filter");

        Assert.Empty(idResult.ProductIds);
        Assert.Empty(documentResult.Documents);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task Health_UsesGenerationAwareIndexHealthContract() {
        CapturingHttpMessageHandler handler = new(throwOnRequest: true, responseJson: "");
        Mock<IElasticsearchIndexService> indexService = new(MockBehavior.Strict);
        indexService.Setup(service => service.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        ElasticsearchProductSearchService service = new(
            new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch/") },
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            Mock.Of<ISearchServingGenerationResolver>(),
            indexService.Object,
            new SearchTextProcessor(),
            NullLogger<ElasticsearchProductSearchService>.Instance);

        bool healthy = await service.IsHealthyAsync();

        Assert.False(healthy);
        Assert.Equal(0, handler.RequestCount);
        indexService.VerifyAll();
    }

    [Theory]
    [InlineData("FENIX:id-AA")]
    [InlineData("fenix:AA")]
    [InlineData("amg:id-ZZ")]
    [InlineData("fenix:id-AA|code-1|code-2")]
    [InlineData("fenix:code-+1")]
    [InlineData("fenix:code-01")]
    [InlineData("fenix:code- 1")]
    public async Task InvalidCatalogSourceGrammar_FailsBeforeReadingGenerationOrElasticsearch(
        string source) {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: false,
            source: source);
        CapturingHttpMessageHandler handler = new(throwOnRequest: true, responseJson: "");
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResultWithDocs result = await service.SearchWithDocsAsync("filter", context);

        Assert.Empty(result.Documents);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task Search_RequestWindowIsBoundedAndOversizedQueryFailsClosed() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: false);
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(context));
        ElasticsearchProductSearchService service = CreateSearchService(handler);

        ProductSearchResultWithDocs bounded = await service.SearchWithDocsAsync(
            "filter",
            context,
            limit: int.MaxValue,
            offset: int.MaxValue);

        Assert.Single(bounded.Documents);
        using (JsonDocument request = JsonDocument.Parse(Assert.IsType<string>(handler.RequestBody))) {
            int size = request.RootElement.GetProperty("size").GetInt32();
            int from = request.RootElement.GetProperty("from").GetInt32();
            Assert.Equal(1, size);
            Assert.Equal(ElasticsearchProductSearchService.MaxResultWindow - 1, from);
            Assert.True(from + size <= ElasticsearchProductSearchService.MaxResultWindow);
        }

        int requestCount = handler.RequestCount;
        ProductSearchResultWithDocs oversized = await service.SearchWithDocsAsync(
            new string('x', ElasticsearchProductSearchService.MaxQueryLength + 1),
            context);
        Assert.Empty(oversized.Documents);
        Assert.Equal(requestCount, handler.RequestCount);
    }

    [Fact]
    public async Task MissingActiveGenerationPointer_FailsClosedWithoutQueryingAnyIndex() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: true);
        CapturingHttpMessageHandler handler = new(
            throwOnRequest: true,
            responseJson: "");
        Mock<ISearchSyncStateStore> stateStore = new(MockBehavior.Strict);
        stateStore.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SearchActiveGeneration?)null);
        ElasticsearchProductSearchService service = new(
            new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch/") },
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            CreateResolver(stateStore.Object),
            Mock.Of<IElasticsearchIndexService>(),
            new SearchTextProcessor(),
            NullLogger<ElasticsearchProductSearchService>.Instance);

        SearchServingUnavailableException exception =
            await Assert.ThrowsAsync<SearchServingUnavailableException>(() =>
                service.SearchWithDocsAsync("filter", context));

        Assert.False(exception.Resolution.HasActiveGeneration);
        Assert.Equal(0, handler.RequestCount);
        stateStore.VerifyAll();
    }

    [Theory]
    [InlineData(ServingStateFailure.Unreadable)]
    [InlineData(ServingStateFailure.MissingWatermark)]
    [InlineData(ServingStateFailure.StaleWatermark)]
    [InlineData(ServingStateFailure.IncompleteCatchUp)]
    [InlineData(ServingStateFailure.StaleSchema)]
    public async Task UnavailableDurableServingState_RejectsSearchBeforeReadingGenerationIndex(
        ServingStateFailure failure) {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: true);
        CapturingHttpMessageHandler handler = new(
            throwOnRequest: true,
            responseJson: string.Empty);
        Mock<ISearchSyncStateStore> stateStore = StateStoreFor(failure);
        ElasticsearchProductSearchService service = new(
            new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch/") },
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            CreateResolver(stateStore.Object),
            Mock.Of<IElasticsearchIndexService>(),
            new SearchTextProcessor(),
            NullLogger<ElasticsearchProductSearchService>.Instance);

        SearchServingUnavailableException exception =
            await Assert.ThrowsAsync<SearchServingUnavailableException>(() =>
                service.SearchWithDocsAsync("filter", context));

        Assert.False(exception.Resolution.IsAvailable);
        Assert.True(exception.Resolution.Stale);
        Assert.Equal(0, handler.RequestCount);
        switch (failure) {
            case ServingStateFailure.Unreadable:
                Assert.False(exception.Resolution.SyncStateReadable);
                break;
            case ServingStateFailure.MissingWatermark:
                Assert.False(exception.Resolution.HasWatermark);
                break;
            case ServingStateFailure.StaleWatermark:
                Assert.True(exception.Resolution.LagSeconds > 300);
                break;
            case ServingStateFailure.IncompleteCatchUp:
                Assert.True(exception.Resolution.IncrementalCatchUpRequired);
                break;
            case ServingStateFailure.StaleSchema:
                Assert.False(exception.Resolution.SchemaCurrent);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(failure), failure, null);
        }
        stateStore.VerifyAll();
    }

    [Theory]
    [InlineData(ServingStateFailure.Unreadable)]
    [InlineData(ServingStateFailure.MissingWatermark)]
    [InlineData(ServingStateFailure.StaleWatermark)]
    [InlineData(ServingStateFailure.IncompleteCatchUp)]
    [InlineData(ServingStateFailure.StaleSchema)]
    public async Task ProductsSearch_UnavailableDurableServingStateReturnsServiceUnavailable(
        ServingStateFailure failure) {
        Mock<IProductService> products = new(MockBehavior.Strict);
        Mock<IElasticsearchProductSearchService> search = new(MockBehavior.Strict);
        Mock<IPriceCacheService> prices = new(MockBehavior.Strict);
        Mock<ISearchSyncStateStore> stateStore = StateStoreFor(failure);
        ProductsController controller = CreateController(
            products,
            search,
            prices,
            syncStateStore: stateStore);

        IActionResult action = await controller.GetAllFromSearchAsync("filter", 20, 0);

        ObjectResult serviceUnavailable = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, serviceUnavailable.StatusCode);
        IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(serviceUnavailable.Value);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        products.VerifyNoOtherCalls();
        search.VerifyNoOtherCalls();
        prices.VerifyNoOtherCalls();
        stateStore.VerifyAll();
    }

    [Theory]
    [InlineData(ServingStateFailure.IncompleteCatchUp)]
    [InlineData(ServingStateFailure.StaleSchema)]
    public async Task ProductsSearch_StateBecomingUnavailableAfterPreflightStillReturnsServiceUnavailable(
        ServingStateFailure failure) {
        ProductPricingContext pricingContext = CreatePricingContext(withVat: false);
        SearchActiveGeneration ready = CreateActiveGeneration(
            pricingContext.DependencyRevisions);
        SearchActiveGeneration unavailable = ready with {
            State = failure switch {
                ServingStateFailure.IncompleteCatchUp => ready.State with {
                    LastIncrementalCatchUpUtc = null
                },
                ServingStateFailure.StaleSchema => ready.State with {
                    SchemaVersion = "legacy-search-schema"
                },
                _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null)
            }
        };
        Mock<ISearchSyncStateStore> stateStore = new(MockBehavior.Strict);
        stateStore.SetupSequence(store =>
                store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ready)
            .ReturnsAsync(unavailable);
        ISearchServingGenerationResolver resolver = CreateResolver(stateStore.Object);
        CapturingHttpMessageHandler handler = new(
            throwOnRequest: true,
            responseJson: string.Empty);
        ElasticsearchProductSearchService searchService = new(
            new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch/") },
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            resolver,
            Mock.Of<IElasticsearchIndexService>(),
            new SearchTextProcessor(),
            NullLogger<ElasticsearchProductSearchService>.Instance);
        Mock<IProductService> products = new(MockBehavior.Strict);
        products.Setup(service => service.GetPricingContext(Guid.Empty, false))
            .Returns(pricingContext);
        Mock<IPriceCacheService> prices = new(MockBehavior.Strict);
        ProductsController controller = new(
            products.Object,
            searchService,
            resolver,
            prices.Object,
            new ResponseFactory()) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            }
        };
        controller.RouteData.Values["culture"] = "uk";

        IActionResult action = await controller.GetAllFromSearchAsync("filter", 20, 0);

        ObjectResult serviceUnavailable = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, serviceUnavailable.StatusCode);
        Assert.Equal(0, handler.RequestCount);
        products.VerifyAll();
        prices.VerifyNoOtherCalls();
        stateStore.Verify(store =>
            store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ChangeTrackingAdvanceAfterGeneration_DoesNotBlockStaticCatalogSearch() {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: true);
        PricingDependencyRevisions staleRevisions = context.PricingRevisions! with {
            ProductPricing = "product:0"
        };
        CapturingHttpMessageHandler handler = new(CreateSearchResponse(context));
        Mock<ISearchSyncStateStore> stateStore = new(MockBehavior.Strict);
        stateStore.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveGeneration(staleRevisions));
        ElasticsearchProductSearchService service = new(
            new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch/") },
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            CreateResolver(stateStore.Object),
            Mock.Of<IElasticsearchIndexService>(),
            new SearchTextProcessor(),
            NullLogger<ElasticsearchProductSearchService>.Instance);

        ProductSearchResultWithDocs result = await service.SearchWithDocsAsync(
            "filter",
            context);

        Assert.Single(result.Documents);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, handler.RequestCount);
        Assert.DoesNotContain("pricingRevisions", handler.RequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("retailPrice", handler.RequestBody, StringComparison.Ordinal);
        stateStore.VerifyAll();
    }

    [Theory]
    [InlineData("products_sync_state")]
    [InlineData("products_2026071401000000x")]
    [InlineData("products_20260714010000000_0123456789abcdef0123456789abcdeg")]
    public async Task InvalidActiveGenerationPointer_FailsClosedWithoutQueryingAnyIndex(
        string indexName) {
        ProductSearchCatalogContext context = CreateCatalogContext(
            withVat: false,
            useIndexedRetailPrice: true);
        CapturingHttpMessageHandler handler = new(
            throwOnRequest: true,
            responseJson: "");
        Mock<ISearchSyncStateStore> stateStore = new(MockBehavior.Strict);
        stateStore.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchActiveGeneration(
                indexName,
                Generation: 1,
                SearchSyncState.Empty));
        ElasticsearchProductSearchService service = new(
            new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch/") },
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            CreateResolver(stateStore.Object),
            Mock.Of<IElasticsearchIndexService>(),
            new SearchTextProcessor(),
            NullLogger<ElasticsearchProductSearchService>.Instance);

        SearchServingUnavailableException exception =
            await Assert.ThrowsAsync<SearchServingUnavailableException>(() =>
                service.SearchWithDocsAsync("filter", context));

        Assert.False(exception.Resolution.HasActiveGeneration);
        Assert.Equal(0, handler.RequestCount);
        stateStore.VerifyAll();
    }

    [Fact]
    public async Task Controller_AnonymousSearchResolvesAndPassesExactVatContextBeforeSearch() {
        ProductPricingContext pricingContext = CreatePricingContext(withVat: true);
        MockSequence sequence = new();
        Mock<IProductService> products = new(MockBehavior.Strict);
        products.InSequence(sequence)
            .Setup(service => service.GetPricingContext(Guid.Empty, true))
            .Returns(pricingContext);
        Mock<IElasticsearchProductSearchService> search = new(MockBehavior.Strict);
        search.InSequence(sequence)
            .Setup(service => service.SearchWithDocsAsync(
                "filter",
                It.Is<ProductSearchCatalogContext>(context =>
                    context.OrganizationId == pricingContext.OrganizationId
                    && context.Source == pricingContext.Source
                    && context.WithVat
                    && context.ClientAgreementNetId == pricingContext.ClientAgreementNetId
                    && context.PricingId == pricingContext.PricingId
                    && context.CurrencyId == pricingContext.CurrencyId
                    && !context.UseIndexedRetailPrice
                    && pricingContext.DependencyRevisions.MatchesExactly(
                        context.PricingRevisions)),
                "uk",
                100,
                0,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSearchResultWithDocs {
                TotalCount = 1,
                Documents = [new ProductSearchDocument { Id = 42, RetailPriceVat = 999m }]
            });
        Mock<IPriceCacheService> prices = new(MockBehavior.Strict);
        prices.Setup(service => service.GetPrices(
                It.Is<List<long>>(ids => ids.SequenceEqual(new long[] { 42 })),
                Guid.Empty,
                pricingContext,
                "uk",
                It.IsAny<Func<List<long>, Dictionary<long, ProductPriceInfo>>>()))
            .Returns(new Dictionary<long, ProductPriceInfo> {
                [42] = new() { Price = 156m, CurrencyCode = "UAH" }
            });
        ProductsController controller = CreateController(products, search, prices);

        IActionResult action = await controller.GetAllFromSearchAsync("filter", 20, 0, withVat: 1);

        List<ProtectedSearchProduct> body = ReadProducts(action);
        ProtectedSearchProduct product = Assert.Single(body);
        Assert.Equal(42, product.Id);
        Assert.Equal("UAH", product.CurrencyCode);
        Assert.Equal(0, product.AvailableQtyUk);
        Assert.Equal(7, product.AvailableQtyUkVAT);
        Assert.Equal(0, product.AvailableQtyPl);
        Assert.Equal(3, product.AvailableQtyPlVAT);
        products.Verify(service => service.GetPricingContext(Guid.Empty, true), Times.Once);
        prices.VerifyAll();
    }

    [Fact]
    public async Task Controller_AnonymousSearchWithUnavailableChangeTrackingReadsPriceFromDatabase() {
        ProductPricingContext pricingContext = CreatePricingContext(
            withVat: false,
            withDurableRevisions: false);
        Mock<IProductService> products = new(MockBehavior.Strict);
        products.Setup(service => service.GetPricingContext(Guid.Empty, false))
            .Returns(pricingContext);
        products.Setup(service => service.GetPricesOnly(
                It.Is<List<long>>(ids => ids.SequenceEqual(new long[] { 42 })),
                pricingContext,
                "uk"))
            .Returns(new Dictionary<long, ProductPriceInfo> {
                [42] = new() { Price = 210m, CurrencyCode = "PLN" }
            });
        Mock<IElasticsearchProductSearchService> search = new(MockBehavior.Strict);
        search.Setup(service => service.SearchWithDocsAsync(
                "filter",
                It.Is<ProductSearchCatalogContext>(context => !context.UseIndexedRetailPrice),
                "uk",
                100,
                0,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSearchResultWithDocs {
                TotalCount = 1,
                Documents = [new ProductSearchDocument { Id = 42, RetailPrice = 999m }]
            });
        Mock<IPriceCacheService> prices = new(MockBehavior.Strict);
        prices.Setup(service => service.GetPrices(
                It.Is<List<long>>(ids => ids.SequenceEqual(new long[] { 42 })),
                Guid.Empty,
                pricingContext,
                "uk",
                It.IsAny<Func<List<long>, Dictionary<long, ProductPriceInfo>>>()))
            .Returns((
                List<long> ids,
                Guid clientNetId,
                ProductPricingContext context,
                string locale,
                Func<List<long>, Dictionary<long, ProductPriceInfo>> fetchFromDb) => fetchFromDb(ids));
        ProductsController controller = CreateController(products, search, prices);

        IActionResult action = await controller.GetAllFromSearchAsync("filter", 20, 0);

        ProtectedSearchProduct product = Assert.Single(ReadProducts(action));
        Assert.Equal(42, product.Id);
        Assert.Equal("PLN", product.CurrencyCode);
        string protectedPrice = Assert.IsType<string>(product.P);
        string decodedPrice = DecodeProtectedPrices(protectedPrice);
        Assert.StartsWith("210.00,", decodedPrice, StringComparison.Ordinal);
        // Compare the price section only: the payload's trailing timestamp can contain the
        // indexed-price digits by coincidence and would make this assertion lie.
        string decodedPrices = decodedPrice.Split('|')[0];
        Assert.DoesNotContain("999", decodedPrices, StringComparison.Ordinal);
        products.VerifyAll();
        search.VerifyAll();
        prices.VerifyAll();
    }

    [Fact]
    public async Task Controller_AnonymousSearchWithNewerChangeTrackingReadsPriceFromDatabase() {
        ProductPricingContext pricingContext = CreatePricingContext(withVat: false);
        PricingDependencyRevisions staleRevisions = pricingContext.DependencyRevisions with {
            ProductPricing = "product:0"
        };
        Mock<IProductService> products = new(MockBehavior.Strict);
        products.Setup(service => service.GetPricingContext(Guid.Empty, false))
            .Returns(pricingContext);
        products.Setup(service => service.GetPricesOnly(
                It.Is<List<long>>(ids => ids.SequenceEqual(new long[] { 42L })),
                pricingContext,
                "uk"))
            .Returns(new Dictionary<long, ProductPriceInfo> {
                [42] = new() { Price = 210m, CurrencyCode = "UAH" }
            });
        Mock<IElasticsearchProductSearchService> search = new(MockBehavior.Strict);
        search.Setup(service => service.SearchWithDocsAsync(
                "filter",
                It.Is<ProductSearchCatalogContext>(context => !context.UseIndexedRetailPrice),
                "uk",
                100,
                0,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSearchResultWithDocs {
                TotalCount = 1,
                Documents = [new ProductSearchDocument { Id = 42, RetailPrice = 999m }]
            });
        Mock<IPriceCacheService> prices = new(MockBehavior.Strict);
        prices.Setup(service => service.GetPrices(
                It.Is<List<long>>(ids => ids.SequenceEqual(new long[] { 42L })),
                Guid.Empty,
                pricingContext,
                "uk",
                It.IsAny<Func<List<long>, Dictionary<long, ProductPriceInfo>>>()))
            .Returns((
                List<long> ids,
                Guid _,
                ProductPricingContext _,
                string _,
                Func<List<long>, Dictionary<long, ProductPriceInfo>> fetchFromDb) =>
                fetchFromDb(ids));
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveGeneration(staleRevisions));
        ProductsController controller = CreateController(
            products,
            search,
            prices,
            syncStateStore: state);

        IActionResult action = await controller.GetAllFromSearchAsync("filter", 20, 0);

        ProtectedSearchProduct product = Assert.Single(ReadProducts(action));
        string protectedPrice = Assert.IsType<string>(product.P);
        string decodedPrice = DecodeProtectedPrices(protectedPrice);
        Assert.StartsWith("210.00,", decodedPrice, StringComparison.Ordinal);
        // Compare the price section only: the payload's trailing timestamp can contain the
        // indexed-price digits by coincidence and would make this assertion lie.
        string decodedPrices = decodedPrice.Split('|')[0];
        Assert.DoesNotContain("999", decodedPrices, StringComparison.Ordinal);
        state.VerifyAll();
        products.VerifyAll();
        search.VerifyAll();
        prices.VerifyAll();
    }

    [Fact]
    public async Task PublicElasticsearchSearch_ResolvesAnonymousContextInsteadOfUsingContextFreeSearch() {
        ProductPricingContext pricingContext = CreatePricingContext(withVat: true);
        Mock<IProductService> products = new(MockBehavior.Strict);
        products.Setup(service => service.GetPricingContext(Guid.Empty, true))
            .Returns(pricingContext);
        Mock<IElasticsearchProductSearchService> search = new(MockBehavior.Strict);
        search.Setup(service => service.SearchAsync(
                "filter",
                It.Is<ProductSearchCatalogContext>(context =>
                    context.OrganizationId == pricingContext.OrganizationId
                    && context.Source == pricingContext.Source
                    && context.WithVat
                    && !context.UseIndexedRetailPrice
                    && pricingContext.DependencyRevisions.MatchesExactly(
                        context.PricingRevisions)),
                "uk",
                7,
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSearchResult { ProductIds = [42], TotalCount = 1 });
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveGeneration(SearchIndexSchema.LiveHydrationMarker));
        ElasticsearchController controller = new(
            Mock.Of<IElasticsearchIndexService>(),
            Mock.Of<IElasticsearchSyncService>(),
            search.Object,
            CreateResolver(state.Object),
            products.Object,
            Mock.Of<IOutputCacheStore>(),
            new ResponseFactory()) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            }
        };
        controller.RouteData.Values["culture"] = "uk";

        IActionResult action = await controller.SearchAsync("filter", 7, 3, withVat: 1);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(action);
        IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(ok.Value);
        ProductSearchResult body = Assert.IsType<ProductSearchResult>(response.Body);
        Assert.Equal(new long[] { 42 }, body.ProductIds);
        search.VerifyAll();
        state.VerifyAll();
    }

    [Fact]
    public async Task PublicElasticsearchSearch_WithUnreadableSyncStateReturnsServiceUnavailable() {
        ProductPricingContext pricingContext = CreatePricingContext(
            withVat: false,
            withDurableRevisions: false);
        Mock<IProductService> products = new(MockBehavior.Strict);
        products.Setup(service => service.GetPricingContext(Guid.Empty, false))
            .Returns(pricingContext);
        Mock<IElasticsearchProductSearchService> search = new(MockBehavior.Strict);
        Mock<ISearchSyncStateStore> state = new(MockBehavior.Strict);
        state.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("generation-control unavailable"));
        ElasticsearchController controller = new(
            Mock.Of<IElasticsearchIndexService>(),
            Mock.Of<IElasticsearchSyncService>(),
            search.Object,
            CreateResolver(state.Object),
            products.Object,
            Mock.Of<IOutputCacheStore>(),
            new ResponseFactory()) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            }
        };

        IActionResult action = await controller.SearchAsync("filter", 20, 0);

        ObjectResult unavailable = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(unavailable.Value);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        search.VerifyNoOtherCalls();
        products.VerifyNoOtherCalls();
        state.VerifyAll();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ElasticsearchSearchPaths_FullRebuildBeforeCatchUpReturnsServiceUnavailable(
        bool debugPath) {
        Mock<IProductService> products = new(MockBehavior.Strict);
        Mock<IElasticsearchProductSearchService> search = new(MockBehavior.Strict);
        Mock<ISearchSyncStateStore> state = StateStoreFor(
            ServingStateFailure.IncompleteCatchUp);
        ElasticsearchController controller = new(
            Mock.Of<IElasticsearchIndexService>(),
            Mock.Of<IElasticsearchSyncService>(),
            search.Object,
            CreateResolver(state.Object),
            products.Object,
            Mock.Of<IOutputCacheStore>(),
            new ResponseFactory()) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            }
        };

        IActionResult action = debugPath
            ? await controller.SearchDebugAsync("filter")
            : await controller.SearchAsync("filter");

        ObjectResult unavailable = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(unavailable.Value);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        products.VerifyNoOtherCalls();
        search.VerifyNoOtherCalls();
        state.VerifyAll();
    }

    [Fact]
    public void AdminDirectIndexEndpoints_AreGenerationGatedAndDoNotMutateElasticsearch() {
        Mock<IElasticsearchIndexService> index = new(MockBehavior.Strict);
        Mock<IOutputCacheStore> outputCache = new(MockBehavior.Strict);
        ElasticsearchController controller = new(
            index.Object,
            Mock.Of<IElasticsearchSyncService>(),
            Mock.Of<IElasticsearchProductSearchService>(),
            Mock.Of<ISearchServingGenerationResolver>(),
            Mock.Of<IProductService>(),
            outputCache.Object,
            new ResponseFactory());

        ConflictObjectResult create = Assert.IsType<ConflictObjectResult>(controller.CreateIndex());
        ConflictObjectResult delete = Assert.IsType<ConflictObjectResult>(controller.DeleteIndex());

        Assert.Equal(StatusCodes.Status409Conflict, create.StatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, delete.StatusCode);
        index.VerifyNoOtherCalls();
        outputCache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Controller_AnonymousSearchWithoutValidatedRetailContextFailsBeforeElasticsearch() {
        Mock<IProductService> products = new(MockBehavior.Strict);
        products.Setup(service => service.GetPricingContext(Guid.Empty, false))
            .Returns((ProductPricingContext)null!);
        Mock<IElasticsearchProductSearchService> search = new(MockBehavior.Strict);
        Mock<IPriceCacheService> prices = new(MockBehavior.Strict);
        ProductsController controller = CreateController(products, search, prices);

        IActionResult action = await controller.GetAllFromSearchAsync("filter", 20, 0);

        Assert.Empty(ReadProducts(action));
        search.VerifyNoOtherCalls();
        prices.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Controller_AnonymousSearchWithWrongVatContextFailsBeforeElasticsearch() {
        Mock<IProductService> products = new(MockBehavior.Strict);
        products.Setup(service => service.GetPricingContext(Guid.Empty, true))
            .Returns(CreatePricingContext(withVat: false));
        Mock<IElasticsearchProductSearchService> search = new(MockBehavior.Strict);
        Mock<IPriceCacheService> prices = new(MockBehavior.Strict);
        ProductsController controller = CreateController(products, search, prices);

        IActionResult action = await controller.GetAllFromSearchAsync("filter", 20, 0, withVat: 1);

        Assert.Empty(ReadProducts(action));
        search.VerifyNoOtherCalls();
        prices.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Controller_AuthenticatedEligibilityIsAppliedBeforeRequestedPage() {
        Guid clientNetId = Guid.NewGuid();
        ProductPricingContext pricingContext = CreatePricingContext(withVat: false);
        Mock<IProductService> products = new(MockBehavior.Strict);
        products.Setup(service => service.GetPricingContext(clientNetId, false))
            .Returns(pricingContext);
        Mock<IElasticsearchProductSearchService> search = new(MockBehavior.Strict);
        search.Setup(service => service.SearchWithDocsAsync(
                "filter",
                It.Is<ProductSearchCatalogContext>(context => !context.UseIndexedRetailPrice),
                "uk",
                100,
                0,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSearchResultWithDocs {
                TotalCount = 102,
                Documents = Enumerable.Range(1, 100)
                    .Select(id => new ProductSearchDocument { Id = id })
                    .ToList()
            });
        search.Setup(service => service.SearchWithDocsAsync(
                "filter",
                It.Is<ProductSearchCatalogContext>(context => !context.UseIndexedRetailPrice),
                "uk",
                100,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSearchResultWithDocs {
                TotalCount = 102,
                Documents = [
                    new ProductSearchDocument { Id = 101 },
                    new ProductSearchDocument { Id = 102 }
                ]
            });
        Mock<IPriceCacheService> prices = new(MockBehavior.Strict);
        prices.Setup(service => service.GetPrices(
                It.Is<List<long>>(ids => ids.SequenceEqual(Enumerable.Range(1, 100).Select(id => (long)id))),
                clientNetId,
                pricingContext,
                "uk",
                It.IsAny<Func<List<long>, Dictionary<long, ProductPriceInfo>>>()))
            .Returns([]);
        prices.Setup(service => service.GetPrices(
                It.Is<List<long>>(ids => ids.SequenceEqual(new long[] { 101, 102 })),
                clientNetId,
                pricingContext,
                "uk",
                It.IsAny<Func<List<long>, Dictionary<long, ProductPriceInfo>>>()))
            .Returns(new Dictionary<long, ProductPriceInfo> {
                [101] = new() { Price = 120m, CurrencyCode = "UAH" },
                [102] = new() { Price = 121m, CurrencyCode = "UAH" }
            });
        ProductsController controller = CreateController(products, search, prices, clientNetId);

        IActionResult action = await controller.GetAllFromSearchAsync("filter", 2, 0);

        Assert.Equal(new long[] { 101, 102 }, ReadProducts(action).Select(product => product.Id));
        search.VerifyAll();
        prices.VerifyAll();
    }

    [Fact]
    public async Task Controller_AuthenticatedEligibilityScan_IsBoundedAndFailsClosed() {
        Guid clientNetId = Guid.NewGuid();
        ProductPricingContext pricingContext = CreatePricingContext(withVat: false);
        Mock<IProductService> products = new(MockBehavior.Strict);
        products.Setup(service => service.GetPricingContext(clientNetId, false))
            .Returns(pricingContext);
        Mock<IElasticsearchProductSearchService> search = new(MockBehavior.Strict);
        search.Setup(service => service.SearchWithDocsAsync(
                "filter",
                It.Is<ProductSearchCatalogContext>(context => !context.UseIndexedRetailPrice),
                "uk",
                100,
                It.Is<int>(offset => offset >= 0 && offset < 10_000),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                string query,
                ProductSearchCatalogContext context,
                string locale,
                int limit,
                int offset,
                CancellationToken cancellationToken) => new ProductSearchResultWithDocs {
                    TotalCount = 20_000,
                    Documents = Enumerable.Range(offset + 1, limit)
                        .Select(id => new ProductSearchDocument { Id = id })
                        .ToList()
                });
        Mock<IPriceCacheService> prices = new(MockBehavior.Strict);
        prices.Setup(service => service.GetPrices(
                It.Is<List<long>>(ids => ids.Count == 100),
                clientNetId,
                pricingContext,
                "uk",
                It.IsAny<Func<List<long>, Dictionary<long, ProductPriceInfo>>>()))
            .Returns([]);
        ProductsController controller = CreateController(products, search, prices, clientNetId);

        IActionResult action = await controller.GetAllFromSearchAsync("filter", 20, 0);

        Assert.Empty(ReadProducts(action));
        search.Verify(service => service.SearchWithDocsAsync(
            "filter",
            It.IsAny<ProductSearchCatalogContext>(),
            "uk",
            100,
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Exactly(100));
        prices.Verify(service => service.GetPrices(
            It.IsAny<List<long>>(),
            clientNetId,
            pricingContext,
            "uk",
            It.IsAny<Func<List<long>, Dictionary<long, ProductPriceInfo>>>()), Times.Exactly(100));
    }

    /// <summary>
    /// Mirrors the AES-CBC unwrap the storefront performs on the protected price payload,
    /// so the tests assert the real wire format instead of plaintext prices.
    /// </summary>
    private static string DecodeProtectedPrices(string encoded) {
        string base64 = encoded.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        using System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Text.Encoding.UTF8.GetBytes(GBA.Common.Configuration.SecuritySettings.Instance.PriceEncryptionKey);
        aes.IV = System.Text.Encoding.UTF8.GetBytes(GBA.Common.Configuration.SecuritySettings.Instance.PriceEncryptionIV);
        using System.Security.Cryptography.ICryptoTransform decryptor = aes.CreateDecryptor();

        byte[] cipher = Convert.FromBase64String(base64);
        byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return System.Text.Encoding.UTF8.GetString(plain);
    }

    private static ProductSearchCatalogContext CreateCatalogContext(
        bool withVat,
        bool useIndexedRetailPrice,
        string source = "fenix") {
        return new ProductSearchCatalogContext(
            OrganizationId: 17,
            Source: source,
            WithVat: withVat,
            ClientAgreementNetId: Guid.NewGuid(),
            PricingId: withVat ? 848 : 853,
            CurrencyId: 1,
            UseIndexedRetailPrice: useIndexedRetailPrice,
            PricingRevisions: useIndexedRetailPrice
                ? TestPricingRevisions()
                : null);
    }

    private static ProductPricingContext CreatePricingContext(
        bool withVat,
        bool withDurableRevisions = true) {
        return new ProductPricingContext(
            Guid.NewGuid(),
            OrganizationId: 17,
            WithVat: withVat,
            Source: "fenix",
            CurrencyId: 1,
            PricingId: withVat ? 848 : 853,
            SelectionVersion: 10,
            DefinitionVersion: 11,
            ProductPricingRevision: withDurableRevisions ? "product:1" : "",
            PricingHierarchyRevision: withDurableRevisions ? "pricing:1" : "",
            DiscountRevision: withDurableRevisions ? "discount:1" : "",
            ExchangeRateRevision: withDurableRevisions ? "rate:1" : "");
    }

    private static string CreateSearchResponse(
        ProductSearchCatalogContext context,
        decimal retailPrice = 156m,
        string? documentSource = null,
        string? documentProductSource = null,
        string? secondaryProductSource = null,
        bool documentIsCanonical = true,
        long? documentOrganizationId = null,
        Guid? documentAgreementNetUid = null,
        int total = 1,
        string totalRelation = "eq") {
        string source = documentSource ?? context.Source;
        string productSource = documentProductSource ?? $"{source}:id-AA|code-11";
        Guid agreementNetUid = documentAgreementNetUid ?? context.ClientAgreementNetId;
        object payload = new {
            took = 3,
            hits = new {
                total = new { value = total, relation = totalRelation },
                hits = new[] {
                    new {
                        _source = new {
                            id = 42,
                            netUid = Guid.NewGuid().ToString(),
                            isForWeb = true,
                            retailPrice,
                            retailPriceVat = retailPrice,
                            retailCurrencyCode = "UAH",
                            indexedProductPricingRevision = context.PricingRevisions?.ProductPricing,
                            indexedPricingHierarchyRevision = context.PricingRevisions?.PricingHierarchy,
                            indexedDiscountRevision = context.PricingRevisions?.Discounts,
                            indexedExchangeRateRevision = context.PricingRevisions?.ExchangeRates,
                            catalogOrganizationIdNonVat = documentOrganizationId ?? context.OrganizationId,
                            catalogOrganizationIdVat = documentOrganizationId ?? context.OrganizationId,
                            catalogAgreementSourceNonVat = context.WithVat ? "fenix:OTHER" : $"{source}:agreement-non-vat",
                            catalogAgreementSourceVat = context.WithVat ? $"{source}:agreement-vat" : "fenix:OTHER",
                            productSourceFenix = productSource.StartsWith("fenix:", StringComparison.Ordinal)
                                ? productSource
                                : secondaryProductSource?.StartsWith("fenix:", StringComparison.Ordinal) == true
                                    ? secondaryProductSource
                                : string.Empty,
                            isCanonicalFenix = documentIsCanonical
                                               && productSource.StartsWith("fenix:", StringComparison.Ordinal),
                            isCanonicalAmg = documentIsCanonical
                                             && productSource.StartsWith("amg:", StringComparison.Ordinal),
                            productSourceAmg = productSource.StartsWith("amg:", StringComparison.Ordinal)
                                ? productSource
                                : secondaryProductSource?.StartsWith("amg:", StringComparison.Ordinal) == true
                                    ? secondaryProductSource
                                : string.Empty,
                            catalogScopes = new[] {
                                new {
                                    organizationId = documentOrganizationId ?? context.OrganizationId,
                                    sourceSystem = source,
                                    withVat = context.WithVat,
                                    availableQtyUk = 7.0,
                                    availableQtyPl = 3.0,
                                    availableQty = 10.0
                                }
                            },
                            catalogSourceSystemNonVat = context.WithVat ? "fenix" : source,
                            catalogSourceSystemVat = context.WithVat ? source : "fenix",
                            catalogAgreementNetUidNonVat = agreementNetUid.ToString(),
                            catalogAgreementNetUidVat = agreementNetUid.ToString(),
                            catalogPricingIdNonVat = context.PricingId,
                            catalogPricingIdVat = context.PricingId,
                            catalogCurrencyIdNonVat = context.CurrencyId,
                            catalogCurrencyIdVat = context.CurrencyId,
                            hasNonVatCatalogSource = !context.WithVat,
                            hasVatCatalogSource = context.WithVat,
                            hasNonVatCatalogAvailability = !context.WithVat,
                            hasVatCatalogAvailability = context.WithVat
                        }
                    }
                }
            }
        };
        return JsonSerializer.Serialize(payload);
    }

    private static ElasticsearchProductSearchService CreateSearchService(HttpMessageHandler handler) {
        HttpClient client = new(handler) { BaseAddress = new Uri("http://elasticsearch/") };
        Mock<ISearchSyncStateStore> stateStore = new(MockBehavior.Strict);
        stateStore.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchActiveGeneration(
                "products_20260714030000000",
                1,
                CreateReadySyncState(SearchIndexSchema.LiveHydrationMarker)));
        return new ElasticsearchProductSearchService(
            client,
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            CreateResolver(stateStore.Object),
            Mock.Of<IElasticsearchIndexService>(),
            new SearchTextProcessor(),
            NullLogger<ElasticsearchProductSearchService>.Instance);
    }

    private static ProductsController CreateController(
        Mock<IProductService> products,
        Mock<IElasticsearchProductSearchService> search,
        Mock<IPriceCacheService> prices,
        Guid? clientNetId = null,
        Mock<ISearchSyncStateStore>? syncStateStore = null) {
        products.Setup(service => service.GetCatalogAvailabilityOnly(
                It.IsAny<List<long>>(),
                It.IsAny<ProductPricingContext>()))
            .Returns((List<long> ids, ProductPricingContext _) => ids.ToDictionary(
                id => id,
                id => new ProductCatalogAvailabilityInfo {
                    Id = id,
                    AvailableQtyUk = 7,
                    AvailableQtyPl = 3,
                    AvailableQty = 10
                }));

        DefaultHttpContext httpContext = new();
        if (clientNetId.HasValue) {
            httpContext.Items[UserNetIdMiddleware.NetIdKey] = clientNetId.Value;
        }

        if (syncStateStore == null) {
            syncStateStore = new Mock<ISearchSyncStateStore>();
            syncStateStore.Setup(store => store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateActiveGeneration(SearchIndexSchema.LiveHydrationMarker));
        }

        ProductsController controller = new(
            products.Object,
            search.Object,
            CreateResolver(syncStateStore.Object),
            prices.Object,
            new ResponseFactory()) {
            ControllerContext = new ControllerContext {
                HttpContext = httpContext,
                RouteData = new RouteData()
            }
        };
        controller.RouteData.Values["culture"] = "uk";
        return controller;
    }

    private static PricingDependencyRevisions TestPricingRevisions() => new(
        "product:1",
        "pricing:1",
        "discount:1",
        "rate:1");

    private static SearchActiveGeneration CreateActiveGeneration(
        PricingDependencyRevisions pricingRevisions) => new(
        "products_20260714030000000",
        1,
        CreateReadySyncState(pricingRevisions));

    private static SearchSyncState CreateReadySyncState(
        PricingDependencyRevisions pricingRevisions) {
        DateTime watermark = DateTime.UtcNow.AddSeconds(-10);
        DateTime rebuildStarted = watermark.AddMinutes(-2);
        DateTime rebuildCompleted = watermark.AddMinutes(-1);
        return new SearchSyncState(
            watermark,
            SearchIndexSchema.CurrentVersion,
            rebuildCompleted,
            RetailConfigurationSignature: "test-config",
            RetailConfigurationEpoch: 1,
            IndexedPricingRevisions: pricingRevisions,
            LastFullRebuildStartedUtc: rebuildStarted,
            LastIncrementalCatchUpUtc: watermark);
    }

    private static ISearchServingGenerationResolver CreateResolver(
        ISearchSyncStateStore stateStore) {
        return new SearchServingGenerationResolver(
            stateStore,
            Options.Create(new SyncSettings { LagWarningSeconds = 300 }),
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            TimeProvider.System);
    }

    private static Mock<ISearchSyncStateStore> StateStoreFor(
        ServingStateFailure failure) {
        Mock<ISearchSyncStateStore> stateStore = new(MockBehavior.Strict);
        var setup = stateStore.Setup(store =>
            store.GetActiveGenerationAsync(It.IsAny<CancellationToken>()));
        if (failure == ServingStateFailure.Unreadable) {
            setup.ThrowsAsync(new HttpRequestException("generation-control unavailable"));
            return stateStore;
        }

        SearchActiveGeneration generation = CreateActiveGeneration(SearchIndexSchema.LiveHydrationMarker);
        SearchSyncState state = failure switch {
            ServingStateFailure.MissingWatermark => generation.State with {
                WatermarkUtc = DateTime.MinValue
            },
            ServingStateFailure.StaleWatermark => CreateStaleSyncState(
                generation.State.IndexedPricingRevisions!),
            ServingStateFailure.IncompleteCatchUp => generation.State with {
                LastIncrementalCatchUpUtc = null
            },
            ServingStateFailure.StaleSchema => generation.State with {
                SchemaVersion = "legacy-search-schema"
            },
            _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null)
        };
        setup.ReturnsAsync(generation with { State = state });
        return stateStore;
    }

    private static SearchSyncState CreateStaleSyncState(
        PricingDependencyRevisions pricingRevisions) {
        DateTime watermark = DateTime.UtcNow.AddMinutes(-10);
        return new SearchSyncState(
            watermark,
            SearchIndexSchema.CurrentVersion,
            LastFullRebuildUtc: watermark.AddMinutes(-1),
            RetailConfigurationSignature: "test-config",
            RetailConfigurationEpoch: 1,
            IndexedPricingRevisions: pricingRevisions,
            LastFullRebuildStartedUtc: watermark.AddMinutes(-2),
            LastIncrementalCatchUpUtc: watermark);
    }

    private static List<ProtectedSearchProduct> ReadProducts(IActionResult action) {
        OkObjectResult ok = Assert.IsType<OkObjectResult>(action);
        IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(ok.Value);
        return Assert.IsType<List<ProtectedSearchProduct>>(response.Body);
    }

    public enum ServingStateFailure {
        Unreadable,
        MissingWatermark,
        StaleWatermark,
        IncompleteCatchUp,
        StaleSchema
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler {
        private readonly bool throwOnRequest;
        private readonly string responseJson;

        public CapturingHttpMessageHandler(string responseJson)
            : this(throwOnRequest: false, responseJson) { }

        public CapturingHttpMessageHandler(bool throwOnRequest, string responseJson) {
            this.throwOnRequest = throwOnRequest;
            this.responseJson = responseJson;
        }

        public int RequestCount { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            RequestCount++;
            if (throwOnRequest) {
                throw new InvalidOperationException("Elasticsearch must not be called without a catalog context.");
            }

            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
