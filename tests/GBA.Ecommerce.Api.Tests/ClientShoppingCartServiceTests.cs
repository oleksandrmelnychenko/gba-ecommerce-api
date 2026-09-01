using System.Data;
using GBA.Common.Search;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Clients;
using Microsoft.Extensions.Http;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ClientShoppingCartServiceTests {
    [Fact]
    public async Task Add_without_an_available_client_or_workplace_agreement_is_a_controlled_bad_request() {
        Guid actorNetId = Guid.NewGuid();
        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IClientAgreementRepository> agreementRepository = new();
        agreementRepository
            .Setup(repository => repository.GetSelectedByClientNetId(actorNetId))
            .Returns((GBA.Domain.Entities.Clients.ClientAgreement)null!);
        agreementRepository
            .Setup(repository => repository.GetSelectedByClientNotSelectedNetId(actorNetId))
            .Returns((GBA.Domain.Entities.Clients.ClientAgreement)null!);

        Mock<IWorkplaceRepository> workplaceRepository = new();
        workplaceRepository
            .Setup(repository => repository.GetByNetId(actorNetId))
            .Returns((GBA.Domain.Entities.Workplace)null!);

        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientAgreementRepository(connection.Object))
            .Returns(agreementRepository.Object);
        clientRepositoriesFactory
            .Setup(factory => factory.NewWorkplaceRepository(connection.Object))
            .Returns(workplaceRepository.Object);

        ClientShoppingCartService service = CreateService(
            connectionFactory.Object,
            clientRepositoriesFactory.Object);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => service.Add(
            new OrderItem { ProductId = 41, Qty = 1 },
            actorNetId,
            withVat: false));

        Assert.Equal("A valid client agreement is required.", exception.Message);
    }

    [Fact]
    public void Cart_context_uses_the_direct_clients_fallback_agreement_when_none_is_selected() {
        Guid actorNetId = Guid.NewGuid();
        ClientAgreement fallbackAgreement = new() {
            NetUid = Guid.NewGuid(),
            Agreement = new Agreement {
                CurrencyId = 11,
                OrganizationId = 22,
                Organization = new Organization { Id = 22 }
            }
        };
        Mock<IClientAgreementRepository> agreementRepository = new();
        agreementRepository
            .Setup(repository => repository.GetSelectedByClientNetId(actorNetId))
            .Returns((ClientAgreement)null!);
        agreementRepository
            .Setup(repository => repository.GetSelectedByClientNotSelectedNetId(actorNetId))
            .Returns(fallbackAgreement);
        Mock<IWorkplaceRepository> workplaceRepository = new();
        workplaceRepository
            .Setup(repository => repository.GetByNetId(actorNetId))
            .Returns((Workplace)null!);

        (ClientAgreement clientAgreement, Workplace workplace) =
            ClientShoppingCartService.ResolveCartContext(
                actorNetId,
                agreementRepository.Object,
                workplaceRepository.Object);

        Assert.Same(fallbackAgreement, clientAgreement);
        Assert.Null(workplace);
        agreementRepository.Verify(
            repository => repository.GetSelectedByWorkplaceNetId(It.IsAny<Guid>()),
            Times.Never);
    }

    private static ClientShoppingCartService CreateService(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) => new(
            clientRepositoriesFactory,
            Mock.Of<ISaleRepositoriesFactory>(),
            Mock.Of<IProductRepositoriesFactory>(),
            Mock.Of<IStorageRepositoryFactory>(),
            Mock.Of<IPricingRepositoriesFactory>(),
            Mock.Of<IExchangeRateRepositoriesFactory>(),
            Mock.Of<ICurrencyRepositoriesFactory>(),
            connectionFactory,
            Mock.Of<IAgreementRepositoriesFactory>(),
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ISearchReindexSignal>());
}
