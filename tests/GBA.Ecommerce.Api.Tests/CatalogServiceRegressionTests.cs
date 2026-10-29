using GBA.Common.Middleware;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Ecommerce.Controllers;
using GBA.Search.Elasticsearch;
using GBA.Search.Models;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.Products.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Products;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class CatalogServiceRegressionTests {
    [Fact]
    public async Task Signed_in_product_page_without_a_selected_agreement_uses_the_fallback_and_writes_nothing() {
        Guid clientNetId = Guid.NewGuid();
        Guid productNetId = Guid.NewGuid();
        ServiceFixture fixture = new();
        ClientAgreement fallback = new() {
            NetUid = Guid.NewGuid(),
            Agreement = new Agreement { OrganizationId = 10561, CurrencyId = 2, WithVATAccounting = true }
        };
        Product expected = new() { NetUid = productNetId, VendorCode = "SEM9401" };
        fixture.ClientAgreementRepository
            .Setup(repository => repository.GetSelectedByClientNotSelectedNetId(clientNetId))
            .Returns(fallback);
        fixture.SingleProductRepository
            .Setup(repository => repository.GetProductByNetId(productNetId, fallback.NetUid, true, 2, 10561))
            .Returns(expected);

        Product actual = await fixture.Service.GetByNetId(productNetId, clientNetId, withVat: false);

        Assert.Same(expected, actual);
        fixture.AgreementRepositoriesFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Signed_in_product_page_for_a_client_without_agreements_does_not_throw() {
        Guid productNetId = Guid.NewGuid();
        ServiceFixture fixture = new();
        Product expected = new() { NetUid = productNetId };
        fixture.SingleProductRepository
            .Setup(repository => repository.GetProductByNetId(productNetId, null, false, null, null))
            .Returns(expected);

        Product actual = await fixture.Service.GetByNetId(productNetId, Guid.NewGuid(), withVat: false);

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task Anonymous_product_page_with_an_empty_id_returns_nothing_instead_of_a_server_error() {
        ServiceFixture fixture = new();

        Product actual = await fixture.Service.GetByNetIdForRetail(Guid.Empty);

        Assert.Null(actual);
        fixture.ConnectionFactory.Verify(factory => factory.NewSqlConnection(), Times.Never);
    }

    [Fact]
    public void Product_service_never_persists_agreement_selection_on_read_paths() {
        string source = File.ReadAllText(RepositoryPath("src/GBA.Services/Services/Products/ProductService.cs"));

        Assert.DoesNotContain("agreementRepository.Update(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IsSelected = true", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Pricing_context_key_ignores_timestamp_only_touches() {
        ClientAgreement clientAgreement = new() { NetUid = Guid.NewGuid() };
        Agreement agreement = new() { CurrencyId = 2, PricingId = 845, OrganizationId = 10561, WithVATAccounting = true, IsActive = true, Updated = DateTime.UtcNow };
        Storage storage = new() { Id = 2968, OrganizationId = 10561, ForVatProducts = true, Locale = "uk", Updated = DateTime.UtcNow };

        string before = ProductService.BuildPricingContextCacheKey(clientAgreement, agreement, storage);
        agreement.Updated = agreement.Updated.AddMinutes(5);
        storage.Updated = storage.Updated.AddMinutes(5);
        string touched = ProductService.BuildPricingContextCacheKey(clientAgreement, agreement, storage);
        agreement.PricingId = 847;
        string repriced = ProductService.BuildPricingContextCacheKey(clientAgreement, agreement, storage);
        agreement.PricingId = 845;
        storage.ForVatProducts = false;
        string restocked = ProductService.BuildPricingContextCacheKey(clientAgreement, agreement, storage);

        Assert.Equal(before, touched);
        Assert.NotEqual(before, repriced);
        Assert.NotEqual(before, restocked);
    }

    [Fact]
    public void Anonymous_analogues_expose_retail_stock_in_both_vat_buckets() {
        List<FromSearchProduct> analogues = [
            new() { AvailableQtyUk = 7 },
            new() { AvailableQtyUk = -3 }
        ];

        ProductService.ExposeRetailAvailabilityInBothStorefrontBuckets(analogues);

        Assert.Equal([7d, 0d], analogues.Select(analogue => analogue.AvailableQtyUkVAT));
        Assert.Equal([7d, 0d], analogues.Select(analogue => analogue.AvailableQtyUk));
    }

    [Fact]
    public void Vendor_code_listing_excludes_deleted_and_non_web_products() {
        Assert.Contains("AND [SearchProduct].Deleted = 0", GetMultipleProductsRepository.CatalogCardsByVendorCodesSql, StringComparison.Ordinal);
        Assert.Contains("AND [SearchProduct].IsForWeb = 1", GetMultipleProductsRepository.CatalogCardsByVendorCodesSql, StringComparison.Ordinal);
        Assert.DoesNotContain("GetCalculatedProduct", GetMultipleProductsRepository.CatalogCardsByVendorCodesSql, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductAvailability", GetMultipleProductsRepository.CatalogCardsByVendorCodesSql, StringComparison.Ordinal);
    }

    [Fact]
    public void Product_detail_prices_are_computed_once_and_road_stock_is_aggregated() {
        string source = File.ReadAllText(RepositoryPath("src/GBA.Domain/Repositories/Products/GetSingleProductRepository.cs"));

        Assert.Equal(1, Count(GetSingleProductRepository.ProductPriceVariablesSql, "GetCalculatedProductPriceWithSharesAndVat("));
        Assert.Equal(1, Count(GetSingleProductRepository.ProductPriceVariablesSql, "GetCalculatedProductLocalPriceWithSharesAndVat("));
        foreach (string method in new[] {
                     MethodBody(source, "public Product GetProductByNetId("),
                     MethodBody(source, "public Product GetByNetIdForRetail(")
                 }) {
            Assert.Contains("ProductPriceVariablesSql +", method, StringComparison.Ordinal);
            Assert.Contains("\", \" + ProductRoadQuantitySql + \" AS [AvailableQtyRoad] \"", method, StringComparison.Ordinal);
            Assert.DoesNotContain("SharesAndVat([Product].NetUID", method, StringComparison.Ordinal);
            Assert.DoesNotContain("LEFT JOIN [SupplyOrderItem]", method, StringComparison.Ordinal);
            Assert.DoesNotContain("AvailableQtyRoad +=", method, StringComparison.Ordinal);
        }
        Assert.Contains("[RoadSupplyOrderItem].Deleted = 0", GetSingleProductRepository.ProductRoadQuantitySql, StringComparison.Ordinal);
        Assert.Contains("[RoadSupplyOrder].IsFullyPlaced = 0", GetSingleProductRepository.ProductRoadQuantitySql, StringComparison.Ordinal);
        Assert.Contains("[RoadSupplyOrderUkraine].IsPlaced = 0", GetSingleProductRepository.ProductRoadQuantitySql, StringComparison.Ordinal);
    }

    [Fact]
    public void Agreement_fallback_skips_deleted_agreements_and_prefers_active_ones() {
        string source = File.ReadAllText(RepositoryPath("src/GBA.Domain/Repositories/Clients/ClientAgreementRepository.cs"));
        int start = source.IndexOf("private ClientAgreement GetByClientContextNetId", StringComparison.Ordinal);
        string method = source[start..source.IndexOf("public ClientAgreement GetSelectedByWorkplaceNetId", start, StringComparison.Ordinal)];

        Assert.Contains("AND [Agreement].Deleted = 0", method, StringComparison.Ordinal);
        Assert.Contains("[Agreement].IsActive DESC, [ClientAgreement].ID", method, StringComparison.Ordinal);
    }

    private static string MethodBody(string source, string signature) {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, signature);
        int end = source.IndexOf("\n    public ", start + signature.Length, StringComparison.Ordinal);
        return end < 0 ? source[start..] : source[start..end];
    }

    private static int Count(string source, string value) {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0) {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string RepositoryPath(string relativePath) {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null) {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate repository file: {relativePath}");
    }

    [Theory]
    [InlineData(true,0)] [InlineData(true,1)] [InlineData(false,0)] [InlineData(false,1)]
    public async Task Authenticated_detail_search_slug_and_related_cards_share_account_stock_without_switching_agreement_VAT(bool agreementVat,int queryVat) {
        PriceTokenTestSupport.EnsureKeys();
        Guid account=Guid.NewGuid(),productNetId=Guid.Parse("8ba390ef-c4be-4729-b840-00a9e6b8d012");
        ServiceFixture fixture=new();
        var agreement=new ClientAgreement { NetUid=Guid.NewGuid(),Agreement=new Agreement {OrganizationId=42,CurrencyId=2,WithVATAccounting=agreementVat}};
        fixture.ClientAgreementRepository.Setup(x=>x.GetSelectedByClientNotSelectedNetId(account)).Returns(agreement);
        var availabilityRow=new ProductAvailability {Id=18,Amount=99};
        var product=new Product {Id=7,NetUid=productNetId,VendorCode="39999CNT",CurrentPrice=15.25m,CurrentWithVatPrice=18.30m,
            AvailableQtyUk=agreementVat?0:99,AvailableQtyUkVAT=agreementVat?99:0,AvailableQtyPl=13,AvailableQtyRoad=8};
        product.ProductAvailabilities.Add(availabilityRow);
        fixture.SingleProductRepository.Setup(x=>x.GetProductByNetId(productNetId,agreement.NetUid,agreementVat,2,42)).Returns(product);
        fixture.SingleProductRepository.Setup(x=>x.GetNetIdBySlug("39999CNT")).Returns(productNetId);
        fixture.AvailabilityRepository.Setup(x=>x.GetEcommerceSellableQuantities(It.Is<IReadOnlyCollection<long>>(ids=>ids.SequenceEqual(new[]{7L})),null,42))
            .Returns(new Dictionary<long,double>{{7,7.25}});
        var facade=new Mock<IProductService>();
        facade.Setup(x=>x.GetByNetId(productNetId,account,queryVat==1)).Returns(()=>fixture.Service.GetByNetId(productNetId,account,queryVat==1));
        facade.Setup(x=>x.GetPricingContext(account,queryVat==1)).Returns(new ProductPricingContext(agreementVat,"EUR","scope-42"));
        facade.Setup(x=>x.GetSellableQuantities(It.IsAny<List<long>>(),account)).Returns((List<long> ids,Guid client)=>fixture.Service.GetSellableQuantities(ids,client));
        var search=new Mock<IElasticsearchProductSearchService>();
        search.Setup(x=>x.SearchWithDocsAsync("39999CNT","uk",20,0,It.IsAny<CancellationToken>())).ReturnsAsync(new ProductSearchResultWithDocs {Documents=[new(){Id=7,NetUid=productNetId.ToString(),VendorCode="39999CNT",Name="Product"}]});
        var prices=new Mock<IPriceCacheService>();
        prices.Setup(x=>x.GetPrices(It.IsAny<List<long>>(),account,queryVat==1,"uk","scope-42",It.IsAny<Func<List<long>,Dictionary<long,ProductPriceInfo>>>()))
            .Returns(new Dictionary<long,ProductPriceInfo>{{7,new(){Price=15.25m,LocalPrice=15.25m,CurrencyCode="EUR"}}});
        var controller=new ProductsController(facade.Object,search.Object,prices.Object,Mock.Of<IClientResourceAccessService>(),new ResponseFactory());
        var context=new DefaultHttpContext();context.Items[UserNetIdMiddleware.NetIdKey]=account;
        controller.ControllerContext=new ControllerContext{HttpContext=context,RouteData=new Microsoft.AspNetCore.Routing.RouteData()};controller.RouteData.Values["culture"]="uk";
        var detailBody=Assert.IsAssignableFrom<IWebResponse>(Assert.IsType<OkObjectResult>(await controller.GetProductByNetId(productNetId,queryVat)).Value);
        var detail=Assert.IsType<Product>(detailBody.Body);
        var searchBody=Assert.IsAssignableFrom<IWebResponse>(Assert.IsType<OkObjectResult>(await controller.GetAllFromSearchAsync("39999CNT",20,0,queryVat)).Value);
        var card=Assert.Single(Assert.IsType<List<ProtectedSearchProduct>>(searchBody.Body));
        Assert.Equal(productNetId,card.NetUid);Assert.Equal(detail.NetUid,card.NetUid);
        Assert.Equal(7.25,detail.AvailableQtyUk);Assert.Equal(7.25,detail.AvailableQtyUkVAT);
        Assert.Equal(detail.AvailableQtyUk,card.AvailableQtyUk);Assert.Equal(detail.AvailableQtyUkVAT,card.AvailableQtyUkVAT);
        Assert.Equal(15.25m,detail.CurrentPrice);Assert.Equal(18.30m,detail.CurrentWithVatPrice);
        Assert.Same(availabilityRow,Assert.Single(detail.ProductAvailabilities));Assert.Equal(99,availabilityRow.Amount);
        Assert.Equal(13,detail.AvailableQtyPl);Assert.Equal(8,detail.AvailableQtyRoad);
        var slug=await fixture.Service.GetProductBySlug("39999CNT",account,queryVat==1);
        Assert.Equal(card.AvailableQtyUk,slug.AvailableQtyUk);Assert.Equal(card.AvailableQtyUkVAT,slug.AvailableQtyUkVAT);
        var related=Assert.Single(ProductService.WithSellableStock([new(){Id=7}],new(){{7,7.25}}));
        Assert.Equal(card.AvailableQtyUk,related.AvailableQtyUk);Assert.Equal(card.AvailableQtyUkVAT,related.AvailableQtyUkVAT);
        fixture.SingleProductRepository.Verify(x=>x.GetProductByNetId(productNetId,agreement.NetUid,agreementVat,2,42),Times.Exactly(2));
        fixture.AvailabilityRepository.Verify(x=>x.GetEcommerceSellableQuantities(It.Is<IReadOnlyCollection<long>>(ids=>ids.SequenceEqual(new[]{7L})),null,42),Times.Exactly(3));
        fixture.AvailabilityRepository.VerifyNoOtherCalls();fixture.AgreementRepositoriesFactory.VerifyNoOtherCalls();
    }

    [Theory][InlineData(-8d)][InlineData(double.NaN)][InlineData(double.PositiveInfinity)][InlineData(0d)]
    public async Task Authenticated_detail_clamps_invalid_or_exhausted_authoritative_stock_like_related_cards(double amount) {
        Guid account=Guid.NewGuid(),netId=Guid.NewGuid();ServiceFixture fixture=new();
        var agreement=new ClientAgreement {NetUid=Guid.NewGuid(),Agreement=new Agreement {OrganizationId=42,WithVATAccounting=true}};
        fixture.ClientAgreementRepository.Setup(x=>x.GetSelectedByClientNotSelectedNetId(account)).Returns(agreement);
        fixture.SingleProductRepository.Setup(x=>x.GetProductByNetId(netId,agreement.NetUid,true,null,42)).Returns(new Product{Id=7,AvailableQtyUkVAT=99});
        fixture.AvailabilityRepository.Setup(x=>x.GetEcommerceSellableQuantities(It.IsAny<IReadOnlyCollection<long>>(),null,42)).Returns(new Dictionary<long,double>{{7,amount}});
        var detail=await fixture.Service.GetByNetId(netId,account,false);
        var related=Assert.Single(ProductService.WithSellableStock([new(){Id=7}],new(){{7,amount}}));
        Assert.Equal(0,detail.AvailableQtyUk);Assert.Equal(0,detail.AvailableQtyUkVAT);
        Assert.Equal(related.AvailableQtyUk,detail.AvailableQtyUk);Assert.Equal(related.AvailableQtyUkVAT,detail.AvailableQtyUkVAT);
    }

    [Fact] public async Task Authenticated_detail_without_organization_never_reads_other_organization_stock() {
        Guid netId=Guid.NewGuid();ServiceFixture fixture=new();
        fixture.SingleProductRepository.Setup(x=>x.GetProductByNetId(netId,null,false,null,null)).Returns(new Product{Id=7,AvailableQtyUk=99,AvailableQtyUkVAT=88});
        var detail=await fixture.Service.GetByNetId(netId,Guid.NewGuid(),true);
        Assert.Equal(0,detail.AvailableQtyUk);Assert.Equal(0,detail.AvailableQtyUkVAT);
        fixture.AvailabilityRepository.VerifyNoOtherCalls();
    }

    private sealed class ServiceFixture {
        public ServiceFixture() {
            ConnectionFactory.Setup(factory => factory.NewSqlConnection()).Returns(Connection.Object);
            ProductRepositoriesFactory
                .Setup(factory => factory.NewGetSingleProductRepository(Connection.Object))
                .Returns(SingleProductRepository.Object);
            ProductRepositoriesFactory.Setup(factory=>factory.NewProductAvailabilityRepository(Connection.Object)).Returns(AvailabilityRepository.Object);
            ClientRepositoriesFactory
                .Setup(factory => factory.NewClientAgreementRepository(Connection.Object))
                .Returns(ClientAgreementRepository.Object);

            Service = new ProductService(
                ClientRepositoriesFactory.Object,
                ProductRepositoriesFactory.Object,
                Mock.Of<IPricingRepositoriesFactory>(),
                Mock.Of<IExchangeRateRepositoriesFactory>(),
                Mock.Of<ICurrencyRepositoriesFactory>(),
                Mock.Of<IOrganizationRepositoriesFactory>(),
                Mock.Of<IStorageRepositoryFactory>(),
                ConnectionFactory.Object,
                AgreementRepositoriesFactory.Object);
        }

        public ProductService Service { get; }
        public Mock<IDbConnection> Connection { get; } = new();
        public Mock<IDbConnectionFactory> ConnectionFactory { get; } = new();
        public Mock<IProductRepositoriesFactory> ProductRepositoriesFactory { get; } = new();
        public Mock<IClientRepositoriesFactory> ClientRepositoriesFactory { get; } = new();
        public Mock<IProductAvailabilityRepository> AvailabilityRepository { get; } = new();
        public Mock<IGetSingleProductRepository> SingleProductRepository { get; } = new();
        public Mock<IClientAgreementRepository> ClientAgreementRepository { get; } = new();
        public Mock<IAgreementRepositoriesFactory> AgreementRepositoriesFactory { get; } = new();
    }
}
