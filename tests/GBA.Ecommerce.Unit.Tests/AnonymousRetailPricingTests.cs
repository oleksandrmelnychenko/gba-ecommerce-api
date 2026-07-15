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
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Products;
using Moq;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class AnonymousRetailPricingTests {
    [Fact]
    public async Task AnonymousProductPaths_UseResolvedRetailAgreementsInsteadOfEmptyIds() {
        RetailDependencies dependencies = new();
        Mock<IGetSingleProductRepository> singleProducts = new();
        Mock<IGetMultipleProductsRepository> multipleProducts = new();
        Product slugProduct = new() { CurrentPrice = 130m };
        singleProducts
            .Setup(repository => repository.GetBySlug(
                "oil-filter",
                dependencies.NonVatAgreementNetId,
                dependencies.VatAgreementNetId,
                "fenix"))
            .Returns(slugProduct);
        singleProducts
            .Setup(repository => repository.GetByNetIdWithoutIncludes(
                dependencies.ProductNetId,
                "fenix"))
            .Returns(new Product { Id = 42 });
        multipleProducts
            .Setup(repository => repository.GetAllComponentsByProductIdWithCalculatedPrices(
                42,
                dependencies.NonVatAgreementNetId,
                dependencies.VatAgreementNetId,
                dependencies.OrganizationId,
                "fenix"))
            .Returns([new FromSearchProduct { CurrentPrice = 130m }]);
        multipleProducts
            .Setup(repository => repository.GetAllByVendorCodes(
                It.IsAny<IReadOnlyCollection<string>>(),
                dependencies.NonVatAgreementNetId,
                dependencies.VatAgreementNetId,
                "fenix"))
            .Returns([new Product { CurrentPrice = 130m }]);
        dependencies.ProductRepositories
            .Setup(factory => factory.NewGetSingleProductRepository(dependencies.Connection.Object))
            .Returns(singleProducts.Object);
        dependencies.ProductRepositories
            .Setup(factory => factory.NewGetMultipleProductsRepository(dependencies.Connection.Object))
            .Returns(multipleProducts.Object);
        ProductService service = dependencies.CreateProductService();

        Product bySlug = await service.GetProductBySlug("oil-filter", Guid.Empty, false);
        List<FromSearchProduct> components = await service.GetAllComponentsByProductNetId(
            dependencies.ProductNetId,
            Guid.Empty,
            false);
        List<Product> byVendor = await service.GetAllByVendorCodes(["FILTER-1"], Guid.Empty, 20, 0, false);

        Assert.Equal(130m, bySlug.CurrentPrice);
        Assert.Equal(130m, Assert.Single(components).CurrentPrice);
        Assert.Equal(130m, Assert.Single(byVendor).CurrentPrice);
    }

    [Theory]
    [InlineData("FILTER' OR 1=1;--")]
    [InlineData("FILTER'); DROP TABLE dbo.Product;--")]
    public async Task VendorCodePayload_IsPassedToRepositoryAsData(string payload) {
        RetailDependencies dependencies = new();
        Mock<IGetMultipleProductsRepository> multipleProducts = new(MockBehavior.Strict);
        multipleProducts
            .Setup(repository => repository.GetAllByVendorCodes(
                It.Is<IReadOnlyCollection<string>>(codes => codes.SequenceEqual(new[] { payload })),
                dependencies.NonVatAgreementNetId,
                dependencies.VatAgreementNetId,
                "fenix"))
            .Returns([]);
        dependencies.ProductRepositories
            .Setup(factory => factory.NewGetMultipleProductsRepository(dependencies.Connection.Object))
            .Returns(multipleProducts.Object);
        ProductService service = dependencies.CreateProductService();

        List<Product> result = await service.GetAllByVendorCodes(
            [payload],
            Guid.Empty,
            20,
            0,
            false);

        Assert.Empty(result);
        multipleProducts.VerifyAll();
    }

    [Fact]
    public async Task AnonymousCarBrandPaths_UseFenixWorldAcrossDistinctVatAgreements() {
        RetailDependencies dependencies = new();
        Mock<IGetMultipleProductsRepository> multipleProducts = new();
        Mock<ICarBrandRepository> carBrands = new();
        carBrands.Setup(repository => repository.GetByAliasIfExists("ford"))
            .Returns(new CarBrand { Alias = "ford" });
        multipleProducts
            .Setup(repository => repository.GetAllProductsByCarBrandNetId(
                dependencies.CarBrandNetId,
                dependencies.NonVatAgreementNetId,
                dependencies.VatAgreementNetId,
                dependencies.OrganizationId,
                "fenix",
                "fenix",
                20,
                0))
            .Returns([new Product { CurrentPrice = 130m, CurrentWithVatPrice = 156m }]);
        multipleProducts
            .Setup(repository => repository.GetAllProductsByCarBrandNetId(
                "ford",
                dependencies.NonVatAgreementNetId,
                dependencies.VatAgreementNetId,
                dependencies.OrganizationId,
                "fenix",
                "fenix",
                20,
                0))
            .Returns([new Product { CurrentPrice = 130m, CurrentWithVatPrice = 156m }]);
        dependencies.ProductRepositories
            .Setup(factory => factory.NewGetMultipleProductsRepository(dependencies.Connection.Object))
            .Returns(multipleProducts.Object);
        dependencies.ProductRepositories
            .Setup(factory => factory.NewCarBrandRepository(dependencies.Connection.Object))
            .Returns(carBrands.Object);
        CarBrandService service = dependencies.CreateCarBrandService();

        List<Product> byNetId = await service.GetAllProductsFilteredByCarBrand(
            dependencies.CarBrandNetId,
            Guid.Empty,
            20,
            0);
        List<Product> byAlias = await service.GetAllProductsFilteredByCarBrand("ford", Guid.Empty, 20, 0);

        Product netIdProduct = Assert.Single(byNetId);
        Assert.Equal(130m, netIdProduct.CurrentPrice);
        Assert.Equal(156m, netIdProduct.CurrentWithVatPrice);
        Product aliasProduct = Assert.Single(byAlias);
        Assert.Equal(130m, aliasProduct.CurrentPrice);
        Assert.Equal(156m, aliasProduct.CurrentWithVatPrice);
        multipleProducts.VerifyAll();
    }

    [Fact]
    public async Task AnonymousProductById_UsesAgreementMatchingRequestedVatMode() {
        RetailDependencies dependencies = new();
        Mock<IGetSingleProductRepository> singleProducts = new();
        Product expected = new() { CurrentPrice = 156m };
        singleProducts
            .Setup(repository => repository.GetProductByNetId(
                dependencies.ProductNetId,
                dependencies.VatAgreementNetId,
                true,
                1,
                dependencies.OrganizationId,
                "fenix"))
            .Returns(expected);
        dependencies.ProductRepositories
            .Setup(factory => factory.NewGetSingleProductRepository(dependencies.Connection.Object))
            .Returns(singleProducts.Object);
        ProductService service = dependencies.CreateProductService();

        Product result = await service.GetByNetIdForRetail(dependencies.ProductNetId, withVat: true);

        Assert.Same(expected, result);
        dependencies.RetailCatalogSelection.Verify(provider => provider.Resolve(
            dependencies.Connection.Object,
            true), Times.Once);
    }

    [Fact]
    public async Task AnonymousProductById_WhenActiveFenixAgreementIsMissing_FailsClosed() {
        RetailDependencies dependencies = new();
        dependencies.RetailCatalogSelection
            .Setup(provider => provider.Resolve(dependencies.Connection.Object, true))
            .Returns((RetailCatalogSelection)null!);
        Mock<IGetSingleProductRepository> singleProducts = new();
        dependencies.ProductRepositories
            .Setup(factory => factory.NewGetSingleProductRepository(dependencies.Connection.Object))
            .Returns(singleProducts.Object);
        ProductService service = dependencies.CreateProductService();

        Product result = await service.GetByNetIdForRetail(dependencies.ProductNetId, withVat: true);

        Assert.Null(result);
        singleProducts.Verify(repository => repository.GetProductByNetId(
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<bool>(),
            It.IsAny<long?>(),
            It.IsAny<long?>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticatedProductById_SelectsVatAgreementWithinSelectedSourceAndOrganization() {
        RetailDependencies dependencies = new();
        Guid clientNetId = Guid.NewGuid();
        Guid selectedAgreementNetId = Guid.NewGuid();
        Organization organization = new() { Id = dependencies.OrganizationId };
        ClientAgreement selectedAgreement = CreateFenixAgreement(
            selectedAgreementNetId,
            organization,
            withVat: false);
        ClientAgreement vatAgreement = CreateFenixAgreement(
            dependencies.VatAgreementNetId,
            organization,
            withVat: true);
        vatAgreement.Agreement.SourceFenixId = [0xF2];
        dependencies.Agreements
            .Setup(repository => repository.GetSelectedForPricing(clientNetId))
            .Returns(selectedAgreement);
        dependencies.Agreements
            .Setup(repository => repository.GetActiveForPricing(
                clientNetId,
                selectedAgreementNetId,
                dependencies.OrganizationId,
                true,
                "fenix"))
            .Returns(vatAgreement);
        Mock<IGetSingleProductRepository> singleProducts = new();
        Product expected = new() { CurrentPrice = 156m };
        singleProducts
            .Setup(repository => repository.GetProductByNetId(
                dependencies.ProductNetId,
                dependencies.VatAgreementNetId,
                true,
                1,
                dependencies.OrganizationId,
                "fenix"))
            .Returns(expected);
        dependencies.ProductRepositories
            .Setup(factory => factory.NewGetSingleProductRepository(dependencies.Connection.Object))
            .Returns(singleProducts.Object);
        ProductService service = dependencies.CreateProductService();

        Product result = await service.GetByNetId(dependencies.ProductNetId, clientNetId, withVat: true);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task AuthenticatedProductById_WhenVatAgreementComesFromAnotherSource_FailsClosed() {
        RetailDependencies dependencies = new();
        Guid clientNetId = Guid.NewGuid();
        Guid selectedAgreementNetId = Guid.NewGuid();
        Organization organization = new() { Id = dependencies.OrganizationId };
        ClientAgreement selectedAgreement = CreateFenixAgreement(
            selectedAgreementNetId,
            organization,
            withVat: false);
        ClientAgreement crossSourceAgreement = new() {
            NetUid = Guid.NewGuid(),
            Agreement = new Agreement {
                IsActive = true,
                WithVATAccounting = true,
                OrganizationId = organization.Id,
                Organization = organization,
                CurrencyId = 1,
                PricingId = 1,
                SourceAmgId = [2]
            }
        };
        dependencies.Agreements
            .Setup(repository => repository.GetSelectedForPricing(clientNetId))
            .Returns(selectedAgreement);
        dependencies.Agreements
            .Setup(repository => repository.GetActiveForPricing(
                clientNetId,
                selectedAgreementNetId,
                dependencies.OrganizationId,
                true,
                "fenix"))
            .Returns(crossSourceAgreement);
        Mock<IGetSingleProductRepository> singleProducts = new();
        dependencies.ProductRepositories
            .Setup(factory => factory.NewGetSingleProductRepository(dependencies.Connection.Object))
            .Returns(singleProducts.Object);
        ProductService service = dependencies.CreateProductService();

        Product result = await service.GetByNetId(dependencies.ProductNetId, clientNetId, withVat: true);

        Assert.Null(result);
        singleProducts.Verify(repository => repository.GetProductByNetId(
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<bool>(),
            It.IsAny<long?>(),
            It.IsAny<long?>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticatedProductById_DifferentAgreementIdentityUsesSameFenixWorld() {
        RetailDependencies dependencies = new();
        Guid clientNetId = Guid.NewGuid();
        Guid selectedAgreementNetId = Guid.NewGuid();
        Organization organization = new() { Id = dependencies.OrganizationId };
        ClientAgreement selectedAgreement = CreateFenixAgreement(
            selectedAgreementNetId,
            organization,
            withVat: false);
        ClientAgreement otherFenixAgreement = CreateFenixAgreement(
            Guid.NewGuid(),
            organization,
            withVat: true);
        otherFenixAgreement.Agreement.SourceFenixId = [2];

        dependencies.Agreements
            .Setup(repository => repository.GetSelectedForPricing(clientNetId))
            .Returns(selectedAgreement);
        dependencies.Agreements
            .Setup(repository => repository.GetActiveForPricing(
                clientNetId,
                selectedAgreementNetId,
                dependencies.OrganizationId,
                true,
                "fenix"))
            .Returns(otherFenixAgreement);
        Mock<IGetSingleProductRepository> singleProducts = new();
        Product expected = new() { CurrentPrice = 156m };
        singleProducts
            .Setup(repository => repository.GetProductByNetId(
                dependencies.ProductNetId,
                otherFenixAgreement.NetUid,
                true,
                1,
                dependencies.OrganizationId,
                "fenix"))
            .Returns(expected);
        dependencies.ProductRepositories
            .Setup(factory => factory.NewGetSingleProductRepository(dependencies.Connection.Object))
            .Returns(singleProducts.Object);
        ProductService service = dependencies.CreateProductService();

        Product result = await service.GetByNetId(dependencies.ProductNetId, clientNetId, withVat: true);

        Assert.Same(expected, result);
        singleProducts.VerifyAll();
    }

    [Fact]
    public async Task AuthenticatedProductById_UsesAmgWorldAcrossDistinctVatAgreements() {
        RetailDependencies dependencies = new();
        Guid clientNetId = Guid.NewGuid();
        Guid selectedAgreementNetId = Guid.NewGuid();
        Organization organization = new() {
            Id = dependencies.OrganizationId,
            PriceSourceIsAmg = true
        };
        ClientAgreement selectedAgreement = CreateAmgAgreement(
            selectedAgreementNetId,
            organization,
            withVat: false,
            sourceId: [0xA1]);
        ClientAgreement vatAgreement = CreateAmgAgreement(
            dependencies.VatAgreementNetId,
            organization,
            withVat: true,
            sourceId: [0xA2]);
        dependencies.Agreements
            .Setup(repository => repository.GetSelectedForPricing(clientNetId))
            .Returns(selectedAgreement);
        dependencies.Agreements
            .Setup(repository => repository.GetActiveForPricing(
                clientNetId,
                selectedAgreementNetId,
                dependencies.OrganizationId,
                true,
                "amg"))
            .Returns(vatAgreement);
        Mock<IGetSingleProductRepository> singleProducts = new();
        Product expected = new() { CurrentPrice = 125m };
        singleProducts
            .Setup(repository => repository.GetProductByNetId(
                dependencies.ProductNetId,
                dependencies.VatAgreementNetId,
                true,
                1,
                dependencies.OrganizationId,
                "amg"))
            .Returns(expected);
        dependencies.ProductRepositories
            .Setup(factory => factory.NewGetSingleProductRepository(dependencies.Connection.Object))
            .Returns(singleProducts.Object);
        ProductService service = dependencies.CreateProductService();

        Product result = await service.GetByNetId(
            dependencies.ProductNetId,
            clientNetId,
            withVat: true);

        Assert.Same(expected, result);
        singleProducts.VerifyAll();
    }

    private static ClientAgreement CreateFenixAgreement(
        Guid netId,
        Organization organization,
        bool withVat) {
        return new ClientAgreement {
            NetUid = netId,
            Agreement = new Agreement {
                IsActive = true,
                WithVATAccounting = withVat,
                OrganizationId = organization.Id,
                Organization = organization,
                CurrencyId = 1,
                PricingId = 1,
                SourceFenixId = [1]
            }
        };
    }

    private static ClientAgreement CreateAmgAgreement(
        Guid netId,
        Organization organization,
        bool withVat,
        byte[] sourceId) {
        return new ClientAgreement {
            NetUid = netId,
            Agreement = new Agreement {
                IsActive = true,
                WithVATAccounting = withVat,
                OrganizationId = organization.Id,
                Organization = organization,
                CurrencyId = 1,
                PricingId = 1,
                SourceAmgId = sourceId
            }
        };
    }

    private sealed class RetailDependencies {
        public RetailDependencies() {
            ConnectionFactory.Setup(factory => factory.NewSqlConnection()).Returns(Connection.Object);
            StorageRepositories.Setup(factory => factory.NewStorageRepository(Connection.Object)).Returns(Storage.Object);
            Organization organization = new() { Id = OrganizationId };
            Storage.Setup(repository => repository.GetFenixRetailWithHighestPriority(It.IsAny<bool>()))
                .Returns(new Storage {
                    OrganizationId = OrganizationId,
                    Organization = organization,
                    ForEcommerce = true
                });
            ClientRepositories.Setup(factory => factory.NewClientRepository(Connection.Object)).Returns(Clients.Object);
            ClientRepositories.Setup(factory => factory.NewClientAgreementRepository(Connection.Object)).Returns(Agreements.Object);
            Clients.Setup(repository => repository.GetRetailClient()).Returns(new Client { NetUid = RetailClientNetId });
            Agreements
                .Setup(repository => repository.GetActiveRetailFenixByOrganizationId(OrganizationId, false))
                .Returns(CreateFenixAgreement(NonVatAgreementNetId, organization, false));
            Agreements
                .Setup(repository => repository.GetActiveRetailFenixByOrganizationId(OrganizationId, true))
                .Returns(CreateFenixAgreement(VatAgreementNetId, organization, true));
            RetailCatalogSelection
                .Setup(provider => provider.Resolve(Connection.Object, false))
                .Returns(CreateRetailSelection(false));
            RetailCatalogSelection
                .Setup(provider => provider.Resolve(Connection.Object, true))
                .Returns(CreateRetailSelection(true));
        }

        public long OrganizationId { get; } = 17;
        public Guid RetailClientNetId { get; } = Guid.NewGuid();
        public Guid NonVatAgreementNetId { get; } = Guid.NewGuid();
        public Guid VatAgreementNetId { get; } = Guid.NewGuid();
        public Guid ProductNetId { get; } = Guid.NewGuid();
        public Guid CarBrandNetId { get; } = Guid.NewGuid();
        public Mock<IDbConnection> Connection { get; } = new();
        public Mock<IDbConnectionFactory> ConnectionFactory { get; } = new();
        public Mock<IClientRepositoriesFactory> ClientRepositories { get; } = new();
        public Mock<IProductRepositoriesFactory> ProductRepositories { get; } = new();
        public Mock<IStorageRepositoryFactory> StorageRepositories { get; } = new();
        public Mock<IStorageRepository> Storage { get; } = new();
        public Mock<IClientRepository> Clients { get; } = new();
        public Mock<IClientAgreementRepository> Agreements { get; } = new();
        public Mock<IPricingDependencyRevisionProvider> PricingRevisions { get; } = new();
        public Mock<IRetailCatalogSelectionProvider> RetailCatalogSelection { get; } = new();

        public ProductService CreateProductService() {
            PricingRevisions
                .Setup(provider => provider.Get(Connection.Object))
                .Returns(new PricingDependencyRevisions("pp:1", "pricing:1", "discount:1", "rate:1"));

            return new ProductService(
                ClientRepositories.Object,
                ProductRepositories.Object,
                Mock.Of<IPricingRepositoriesFactory>(),
                Mock.Of<IExchangeRateRepositoriesFactory>(),
                Mock.Of<ICurrencyRepositoriesFactory>(),
                Mock.Of<IOrganizationRepositoriesFactory>(),
                StorageRepositories.Object,
                ConnectionFactory.Object,
                Mock.Of<IAgreementRepositoriesFactory>(),
                PricingRevisions.Object,
                RetailCatalogSelection.Object);
        }

        public CarBrandService CreateCarBrandService() {
            PricingRevisions
                .Setup(provider => provider.Get(Connection.Object))
                .Returns(new PricingDependencyRevisions("pp:1", "pricing:1", "discount:1", "rate:1"));

            return new CarBrandService(
                ConnectionFactory.Object,
                ProductRepositories.Object,
                ClientRepositories.Object,
                StorageRepositories.Object,
                PricingRevisions.Object,
                RetailCatalogSelection.Object);
        }

        private RetailCatalogSelection CreateRetailSelection(bool withVat) {
            return new RetailCatalogSelection {
                StorageId = withVat ? 22 : 21,
                OrganizationId = OrganizationId,
                WithVat = withVat,
                StorageUpdated = DateTime.UtcNow,
                ClientAgreementNetUid = withVat ? VatAgreementNetId : NonVatAgreementNetId,
                PricingId = withVat ? 848 : 853,
                CurrencyId = 1,
                SourceFenixId = withVat ? [2] : [1],
                CatalogAgreementSource = withVat ? "fenix:id-02" : "fenix:id-01",
                ClientAgreementUpdated = DateTime.UtcNow,
                AgreementUpdated = DateTime.UtcNow
            };
        }
    }
}
