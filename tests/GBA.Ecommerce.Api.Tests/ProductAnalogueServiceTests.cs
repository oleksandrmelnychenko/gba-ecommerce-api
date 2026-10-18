using System.Data;
using GBA.Domain.DbConnectionFactory.Contracts;
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

namespace GBA.Ecommerce.Api.Tests;

public sealed class ProductAnalogueServiceTests {
    [Fact]
    public async Task Uses_an_active_fallback_agreement_when_none_is_selected() {
        Guid clientNetId = Guid.NewGuid();
        ProductAnalogueServiceFixture fixture = new();
        ClientAgreement fallbackAgreement = CreateAgreement(
            netUid: Guid.NewGuid(),
            organizationId: 11,
            currencyId: 22,
            withVat: true);
        List<FromSearchProduct> expected = [
            new() { NetUid = Guid.NewGuid(), VendorCode = "FE20096" }
        ];

        fixture.ClientAgreementRepository
            .Setup(repository => repository.GetSelectedByClientNetId(clientNetId))
            .Returns((ClientAgreement)null!);
        fixture.ClientAgreementRepository
            .Setup(repository => repository.GetSelectedByWorkplaceNetId(clientNetId))
            .Returns((ClientAgreement)null!);
        fixture.ClientAgreementRepository
            .Setup(repository => repository.GetSelectedByClientNotSelectedNetId(clientNetId))
            .Returns(fallbackAgreement);
        fixture.MultipleProductsRepository
            .Setup(repository => repository.GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPrices(
                fixture.Product.Id,
                fallbackAgreement.NetUid,
                fallbackAgreement.Agreement.OrganizationId,
                fallbackAgreement.Agreement.CurrencyId,
                fallbackAgreement.Agreement.WithVATAccounting))
            .Returns(expected);

        List<FromSearchProduct> actual = await fixture.Service.GetAllAnaloguesByProductNetId(
            fixture.Product.NetUid,
            clientNetId,
            withVat: false);

        FromSearchProduct analogue = Assert.Single(actual);
        Assert.Equal("FE20096", analogue.VendorCode);
        fixture.ClientAgreementRepository.Verify(
            repository => repository.GetSelectedByClientNotSelectedNetId(clientNetId),
            Times.Once);
        fixture.MultipleProductsRepository.VerifyAll();
    }

    [Fact]
    public async Task Keeps_using_the_explicitly_selected_agreement() {
        Guid clientNetId = Guid.NewGuid();
        ProductAnalogueServiceFixture fixture = new();
        ClientAgreement selectedAgreement = CreateAgreement(
            netUid: Guid.NewGuid(),
            organizationId: 33,
            currencyId: 44,
            withVat: false);
        List<FromSearchProduct> expected = [
            new() { NetUid = Guid.NewGuid(), VendorCode = "SABO890850B" }
        ];

        fixture.ClientAgreementRepository
            .Setup(repository => repository.GetSelectedByClientNetId(clientNetId))
            .Returns(selectedAgreement);
        fixture.MultipleProductsRepository
            .Setup(repository => repository.GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPrices(
                fixture.Product.Id,
                selectedAgreement.NetUid,
                selectedAgreement.Agreement.OrganizationId,
                selectedAgreement.Agreement.CurrencyId,
                selectedAgreement.Agreement.WithVATAccounting))
            .Returns(expected);

        List<FromSearchProduct> actual = await fixture.Service.GetAllAnaloguesByProductNetId(
            fixture.Product.NetUid,
            clientNetId,
            withVat: true);

        Assert.Equal("SABO890850B", Assert.Single(actual).VendorCode);
        fixture.ClientAgreementRepository.Verify(
            repository => repository.GetSelectedByWorkplaceNetId(It.IsAny<Guid>()),
            Times.Never);
        fixture.ClientAgreementRepository.Verify(
            repository => repository.GetSelectedByClientNotSelectedNetId(It.IsAny<Guid>()),
            Times.Never);
        fixture.MultipleProductsRepository.VerifyAll();
    }

    private static ClientAgreement CreateAgreement(
        Guid netUid,
        long organizationId,
        long currencyId,
        bool withVat) => new() {
        NetUid = netUid,
        Agreement = new Agreement {
            OrganizationId = organizationId,
            CurrencyId = currencyId,
            WithVATAccounting = withVat
        }
    };

    private sealed class ProductAnalogueServiceFixture {
        public ProductAnalogueServiceFixture() {
            Product = new Product {
                Id = 101,
                NetUid = Guid.NewGuid(),
                VendorCode = "SABO890352B"
            };

            ConnectionFactory
                .Setup(factory => factory.NewSqlConnection())
                .Returns(Connection.Object);
            ProductRepositoriesFactory
                .Setup(factory => factory.NewGetSingleProductRepository(Connection.Object))
                .Returns(SingleProductRepository.Object);
            ProductRepositoriesFactory
                .Setup(factory => factory.NewGetMultipleProductsRepository(Connection.Object))
                .Returns(MultipleProductsRepository.Object);
            ClientRepositoriesFactory
                .Setup(factory => factory.NewClientAgreementRepository(Connection.Object))
                .Returns(ClientAgreementRepository.Object);
            SingleProductRepository
                .Setup(repository => repository.GetByNetIdWithoutIncludes(Product.NetUid))
                .Returns(Product);

            Service = new ProductService(
                ClientRepositoriesFactory.Object,
                ProductRepositoriesFactory.Object,
                Mock.Of<IPricingRepositoriesFactory>(),
                Mock.Of<IExchangeRateRepositoriesFactory>(),
                Mock.Of<ICurrencyRepositoriesFactory>(),
                Mock.Of<IOrganizationRepositoriesFactory>(),
                Mock.Of<IStorageRepositoryFactory>(),
                ConnectionFactory.Object,
                Mock.Of<IAgreementRepositoriesFactory>());
        }

        public Product Product { get; }
        public ProductService Service { get; }
        public Mock<IDbConnection> Connection { get; } = new();
        public Mock<IDbConnectionFactory> ConnectionFactory { get; } = new();
        public Mock<IProductRepositoriesFactory> ProductRepositoriesFactory { get; } = new();
        public Mock<IClientRepositoriesFactory> ClientRepositoriesFactory { get; } = new();
        public Mock<IGetSingleProductRepository> SingleProductRepository { get; } = new();
        public Mock<IGetMultipleProductsRepository> MultipleProductsRepository { get; } = new();
        public Mock<IClientAgreementRepository> ClientAgreementRepository { get; } = new();
    }
}
