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

namespace GBA.Ecommerce.Unit.Tests;

public sealed class RetailAnaloguePricingTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetailAnalogue_UsesRequestedVatAndResolvedAgreementOrganization(bool withVat) {
        RetailAnalogueDependencies dependencies = new(withVat);
        List<FromSearchProduct> expected = [new FromSearchProduct { Id = 84 }];
        dependencies.MultipleProducts
            .Setup(repository => repository.GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
                dependencies.ProductId,
                dependencies.AgreementNetId,
                dependencies.CatalogSource,
                dependencies.OrganizationId,
                dependencies.CurrencyId,
                withVat))
            .Returns(expected);
        ProductService service = dependencies.CreateService();

        List<FromSearchProduct> result = withVat
            ? await service.GetAllAnaloguesByProductNetIdForRetail(dependencies.ProductNetId, true)
            : await service.GetAllAnaloguesByProductNetIdForRetail(dependencies.ProductNetId);

        Assert.Same(expected, result);
        dependencies.MultipleProducts.Verify(repository =>
            repository.GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
                dependencies.ProductId,
                dependencies.AgreementNetId,
                dependencies.CatalogSource,
                dependencies.OrganizationId,
                dependencies.CurrencyId,
                withVat),
            Times.Once);
    }

    [Fact]
    public async Task RetailAnalogue_WhenSharedRetailSelectionIsInvalid_FailsClosed() {
        RetailAnalogueDependencies dependencies = new(withVat: true);
        dependencies.RetailCatalogSelection
            .Setup(provider => provider.Resolve(dependencies.Connection.Object, true))
            .Returns((RetailCatalogSelection)null!);
        ProductService service = dependencies.CreateService();

        List<FromSearchProduct> result = await service.GetAllAnaloguesByProductNetIdForRetail(
            dependencies.ProductNetId,
            true);

        Assert.Empty(result);
        dependencies.MultipleProducts.Verify(repository =>
            repository.GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
                It.IsAny<long>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<long?>(),
                It.IsAny<long?>(),
                It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public void RetailAnalogueRepository_WhenRequiredContextIsMissing_FailsClosedBeforeQuerying() {
        GetMultipleProductsRepository repository = new(null!);

        Assert.Empty(repository.GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
            0,
            Guid.NewGuid(),
            "fenix",
            17,
            1,
            false));
        Assert.Empty(repository.GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
            42,
            Guid.Empty,
            "fenix",
            17,
            1,
            false));
        Assert.Empty(repository.GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
            42,
            Guid.NewGuid(),
            "fenix",
            null,
            1,
            false));
        Assert.Empty(repository.GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
            42,
            Guid.NewGuid(),
            string.Empty,
            0,
            1,
            false));
    }

    [Fact]
    public void RetailAnalogueRepository_QueryUsesExactPricingAndAvailabilityScope() {
        string source = ReadRepositorySource();
        string method = ExtractMethod(
            source,
            "public List<FromSearchProduct> GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(");

        Assert.Contains("AND [Agreement].OrganizationID = @OrganizationId", method, StringComparison.Ordinal);
        Assert.Contains("AND [Agreement].WithVATAccounting = @WithVat", method, StringComparison.Ordinal);
        Assert.Contains("AND [Agreement].IsActive = 1", method, StringComparison.Ordinal);
        Assert.Contains("AND [Organization].Deleted = 0", method, StringComparison.Ordinal);
        Assert.Contains("[Organization].PriceSourceIsAmg = @PriceSourceIsAmg", method, StringComparison.Ordinal);
        Assert.Contains(
            "ProductSourceIdentitySql.CanonicalSourceWorldPredicate(\"[Analogue]\")",
            method,
            StringComparison.Ordinal);
        Assert.Contains("LEFT JOIN [ScopedProductAvailability] AS [ProductAvailability]", method, StringComparison.Ordinal);
        Assert.Contains("AND [Storage].ForEcommerce = 1", method, StringComparison.Ordinal);
        Assert.Contains("AND [Storage].ForDefective = 0", method, StringComparison.Ordinal);
        Assert.Contains("AND [Storage].OrganizationID = @OrganizationId", method, StringComparison.Ordinal);
        Assert.Contains("AND [Storage].ForVatProducts = @WithVat", method, StringComparison.Ordinal);
        Assert.Contains("WithVat = withVat", method, StringComparison.Ordinal);
        Assert.DoesNotContain("WithVat = true", method, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "(([Storage].ForEcommerce = 1 AND [Storage].Deleted = 0) OR [Storage].ID IS NULL)",
            method,
            StringComparison.Ordinal);
    }

    private static string ReadRepositorySource() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory != null) {
            string path = Path.Combine(
                directory.FullName,
                "src",
                "GBA.Domain",
                "Repositories",
                "Products",
                "GetMultipleProductsRepository.cs");

            if (File.Exists(path)) return File.ReadAllText(path);
            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate GetMultipleProductsRepository.cs from {AppContext.BaseDirectory}.");
    }

    private static string ExtractMethod(string source, string signature) {
        int methodStart = source.IndexOf(signature, StringComparison.Ordinal);
        if (methodStart < 0) throw new InvalidOperationException($"Method '{signature}' was not found.");

        int bodyStart = source.IndexOf('{', methodStart);
        if (bodyStart < 0) throw new InvalidOperationException($"Method '{signature}' has no body.");

        int depth = 0;
        for (int index = bodyStart; index < source.Length; index++) {
            switch (source[index]) {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0) return source[methodStart..(index + 1)];
                    break;
            }
        }

        throw new InvalidOperationException($"Method '{signature}' has an unterminated body.");
    }

    private sealed class RetailAnalogueDependencies {
        public RetailAnalogueDependencies(bool withVat) {
            Organization organization = new() { Id = OrganizationId };
            Agreement = new ClientAgreement {
                NetUid = AgreementNetId,
                Agreement = new Agreement {
                    IsActive = true,
                    WithVATAccounting = withVat,
                    OrganizationId = OrganizationId,
                    Organization = organization,
                    CurrencyId = CurrencyId,
                    PricingId = 1,
                    SourceFenixId = [1]
                }
            };

            ConnectionFactory.Setup(factory => factory.NewSqlConnection()).Returns(Connection.Object);
            ClientRepositories
                .Setup(factory => factory.NewClientAgreementRepository(Connection.Object))
                .Returns(Agreements.Object);
            StorageRepositories
                .Setup(factory => factory.NewStorageRepository(Connection.Object))
                .Returns(Storages.Object);
            ProductRepositories
                .Setup(factory => factory.NewGetSingleProductRepository(Connection.Object))
                .Returns(SingleProducts.Object);
            ProductRepositories
                .Setup(factory => factory.NewGetMultipleProductsRepository(Connection.Object))
                .Returns(MultipleProducts.Object);
            Storages
                .Setup(repository => repository.GetFenixRetailWithHighestPriority(withVat))
                .Returns(new Storage {
                    OrganizationId = OrganizationId,
                    Organization = organization,
                    ForEcommerce = true,
                    ForVatProducts = withVat
                });
            Agreements
                .Setup(repository => repository.GetActiveRetailFenixByOrganizationId(OrganizationId, withVat))
                .Returns(Agreement);
            SingleProducts
                .Setup(repository => repository.GetByNetIdWithoutIncludes(ProductNetId, CatalogSource))
                .Returns(new Product { Id = ProductId });
            RetailCatalogSelection
                .Setup(provider => provider.Resolve(Connection.Object, withVat))
                .Returns(new RetailCatalogSelection {
                    StorageId = 21,
                    OrganizationId = OrganizationId,
                    WithVat = withVat,
                    StorageUpdated = DateTime.UtcNow,
                    ClientAgreementNetUid = AgreementNetId,
                    PricingId = 1,
                    CurrencyId = CurrencyId,
                    SourceFenixId = [1],
                    CatalogAgreementSource = "fenix:id-01",
                    ClientAgreementUpdated = DateTime.UtcNow,
                    AgreementUpdated = DateTime.UtcNow
                });
        }

        public long ProductId { get; } = 42;
        public long OrganizationId { get; } = 17;
        public long CurrencyId { get; } = 1;
        public string CatalogSource { get; } = "fenix";
        public Guid ProductNetId { get; } = Guid.NewGuid();
        public Guid AgreementNetId { get; } = Guid.NewGuid();
        public ClientAgreement Agreement { get; }
        public Mock<IDbConnection> Connection { get; } = new();
        public Mock<IDbConnectionFactory> ConnectionFactory { get; } = new();
        public Mock<IClientRepositoriesFactory> ClientRepositories { get; } = new();
        public Mock<IClientAgreementRepository> Agreements { get; } = new();
        public Mock<IStorageRepositoryFactory> StorageRepositories { get; } = new();
        public Mock<IStorageRepository> Storages { get; } = new();
        public Mock<IProductRepositoriesFactory> ProductRepositories { get; } = new();
        public Mock<IGetSingleProductRepository> SingleProducts { get; } = new();
        public Mock<IGetMultipleProductsRepository> MultipleProducts { get; } = new();
        public Mock<IPricingDependencyRevisionProvider> PricingRevisions { get; } = new();
        public Mock<IRetailCatalogSelectionProvider> RetailCatalogSelection { get; } = new();

        public ProductService CreateService() {
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
    }
}
