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

    private sealed class ServiceFixture {
        public ServiceFixture() {
            ConnectionFactory.Setup(factory => factory.NewSqlConnection()).Returns(Connection.Object);
            ProductRepositoriesFactory
                .Setup(factory => factory.NewGetSingleProductRepository(Connection.Object))
                .Returns(SingleProductRepository.Object);
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
        public Mock<IGetSingleProductRepository> SingleProductRepository { get; } = new();
        public Mock<IClientAgreementRepository> ClientAgreementRepository { get; } = new();
        public Mock<IAgreementRepositoriesFactory> AgreementRepositoriesFactory { get; } = new();
    }
}
