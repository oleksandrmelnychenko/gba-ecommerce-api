using System.Data;
using System.Dynamic;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Ecommerce.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Clients;
using GBA.Services.Services.Orders.Contracts;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ClientServiceTests {
    [Fact]
    public async Task Root_profile_resolves_the_main_client_for_a_workplace_identity() {
        Guid workplaceNetId = Guid.NewGuid();
        Client mainClient = new() { NetUid = Guid.NewGuid() };
        mainClient.ClientAgreements.Add(new ClientAgreement {
            Agreement = new Agreement {
                IsSelected = true,
                Currency = new Currency { Code = "EUR" }
            }
        });
        Workplace workplace = new() { MainClient = mainClient };

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IClientRepository> clientRepository = new();
        clientRepository
            .Setup(repository => repository.GetRootClientBySubClientNetId(workplaceNetId))
            .Returns((Client)null!);
        dynamic debtTotals = new ExpandoObject();
        debtTotals.TotalLocal = 120m;
        clientRepository
            .Setup(repository => repository.GetDebtTotalsForClientStructureWithRootByClientNetId(mainClient.NetUid, true))
            .Returns((object)debtTotals);

        Mock<IWorkplaceRepository> workplaceRepository = new();
        workplaceRepository
            .Setup(repository => repository.GetByNetIdWithClient(workplaceNetId))
            .Returns(workplace);

        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(connection.Object))
            .Returns(clientRepository.Object);
        clientRepositoriesFactory
            .Setup(factory => factory.NewWorkplaceRepository(connection.Object))
            .Returns(workplaceRepository.Object);

        Mock<IExchangeRateRepository> exchangeRateRepository = new();
        exchangeRateRepository
            .Setup(repository => repository.GetByCurrencyCodeAndCurrentCulture("EUR"))
            .Returns(new ExchangeRate { Amount = 2m });
        Mock<IExchangeRateRepositoriesFactory> exchangeRateRepositoriesFactory = new();
        exchangeRateRepositoriesFactory
            .Setup(factory => factory.NewExchangeRateRepository(connection.Object))
            .Returns(exchangeRateRepository.Object);

        ClientService service = new(
            clientRepositoriesFactory.Object,
            exchangeRateRepositoriesFactory.Object,
            Mock.Of<IRetailClientRepositoriesFactory>(),
            Mock.Of<IEcommerceAdminPanelRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IOrderService>(),
            Mock.Of<IProductRepositoriesFactory>(),
            Mock.Of<IStorageRepositoryFactory>());

        Client result = await service.GetRootClientBySubClientNerId(workplaceNetId);

        Assert.Same(mainClient, result);
        Assert.Same(workplace, result.CurrentWorkplace);
        Assert.Equal(60m, result.AccountBalance);
    }

    [Fact]
    public async Task Client_without_agreements_falls_back_to_local_debt() {
        Guid clientNetId = Guid.NewGuid();
        Client client = new() { NetUid = clientNetId };

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IClientRepository> clientRepository = new();
        clientRepository
            .Setup(repository => repository.GetRootClientBySubClientNetId(clientNetId))
            .Returns(client);
        dynamic debtTotals = new ExpandoObject();
        debtTotals.TotalLocal = 42.4m;
        clientRepository
            .Setup(repository => repository.GetDebtTotalsForClientStructureWithRootByClientNetId(clientNetId, true))
            .Returns((object)debtTotals);

        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(connection.Object))
            .Returns(clientRepository.Object);

        Mock<IExchangeRateRepositoriesFactory> exchangeRateRepositoriesFactory = new();
        exchangeRateRepositoriesFactory
            .Setup(factory => factory.NewExchangeRateRepository(connection.Object))
            .Returns(Mock.Of<IExchangeRateRepository>());

        ClientService service = new(
            clientRepositoriesFactory.Object,
            exchangeRateRepositoriesFactory.Object,
            Mock.Of<IRetailClientRepositoriesFactory>(),
            Mock.Of<IEcommerceAdminPanelRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IOrderService>(),
            Mock.Of<IProductRepositoriesFactory>(),
            Mock.Of<IStorageRepositoryFactory>());

        Client result = await service.GetRootClientBySubClientNerId(clientNetId);

        Assert.Same(client, result);
        Assert.Equal(42.4m, result.AccountBalance);
    }

    [Fact]
    public async Task Agreement_without_currency_does_not_crash_the_profile() {
        Guid clientNetId = Guid.NewGuid();
        Client client = new() { NetUid = clientNetId };
        client.ClientAgreements.Add(new ClientAgreement { Agreement = new Agreement { IsSelected = true } });

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IClientRepository> clientRepository = new();
        clientRepository
            .Setup(repository => repository.GetRootClientBySubClientNetId(clientNetId))
            .Returns(client);
        dynamic debtTotals = new ExpandoObject();
        debtTotals.TotalLocal = 10m;
        clientRepository
            .Setup(repository => repository.GetDebtTotalsForClientStructureWithRootByClientNetId(clientNetId, true))
            .Returns((object)debtTotals);

        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(connection.Object))
            .Returns(clientRepository.Object);

        Mock<IExchangeRateRepositoriesFactory> exchangeRateRepositoriesFactory = new();
        exchangeRateRepositoriesFactory
            .Setup(factory => factory.NewExchangeRateRepository(connection.Object))
            .Returns(Mock.Of<IExchangeRateRepository>());

        ClientService service = new(
            clientRepositoriesFactory.Object,
            exchangeRateRepositoriesFactory.Object,
            Mock.Of<IRetailClientRepositoriesFactory>(),
            Mock.Of<IEcommerceAdminPanelRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IOrderService>(),
            Mock.Of<IProductRepositoriesFactory>(),
            Mock.Of<IStorageRepositoryFactory>());

        Client result = await service.GetRootClientBySubClientNerId(clientNetId);

        Assert.Equal(10m, result.AccountBalance);
    }

    [Fact]
    public async Task Profile_without_agreements_falls_back_to_local_debt() {
        Guid clientNetId = Guid.NewGuid();
        Client client = new() { NetUid = clientNetId };

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IClientRepository> clientRepository = new();
        clientRepository
            .Setup(repository => repository.GetByNetId(clientNetId, true))
            .Returns(client);
        dynamic debtTotals = new ExpandoObject();
        debtTotals.TotalLocal = 42.4m;
        clientRepository
            .Setup(repository => repository.GetDebtTotalsForClientStructureWithRootByClientNetId(clientNetId, true))
            .Returns((object)debtTotals);

        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(connection.Object))
            .Returns(clientRepository.Object);

        Mock<IExchangeRateRepositoriesFactory> exchangeRateRepositoriesFactory = new();
        exchangeRateRepositoriesFactory
            .Setup(factory => factory.NewExchangeRateRepository(connection.Object))
            .Returns(Mock.Of<IExchangeRateRepository>());

        ClientService service = new(
            clientRepositoriesFactory.Object,
            exchangeRateRepositoriesFactory.Object,
            Mock.Of<IRetailClientRepositoriesFactory>(),
            Mock.Of<IEcommerceAdminPanelRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IOrderService>(),
            Mock.Of<IProductRepositoriesFactory>(),
            Mock.Of<IStorageRepositoryFactory>());

        Client result = await service.GetByNetId(clientNetId);

        Assert.Equal(42.4m, result.AccountBalance);
    }

    [Fact]
    public async Task Unknown_profile_reports_a_controlled_error() {
        Guid unknownNetId = Guid.NewGuid();

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IClientRepository> clientRepository = new();
        clientRepository
            .Setup(repository => repository.GetByNetId(unknownNetId, true))
            .Returns((Client)null!);

        Mock<IWorkplaceRepository> workplaceRepository = new();
        workplaceRepository
            .Setup(repository => repository.GetByNetIdWithClient(unknownNetId))
            .Returns((Workplace)null!);

        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(connection.Object))
            .Returns(clientRepository.Object);
        clientRepositoriesFactory
            .Setup(factory => factory.NewWorkplaceRepository(connection.Object))
            .Returns(workplaceRepository.Object);

        ClientService service = new(
            clientRepositoriesFactory.Object,
            Mock.Of<IExchangeRateRepositoriesFactory>(),
            Mock.Of<IRetailClientRepositoriesFactory>(),
            Mock.Of<IEcommerceAdminPanelRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IOrderService>(),
            Mock.Of<IProductRepositoriesFactory>(),
            Mock.Of<IStorageRepositoryFactory>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByNetId(unknownNetId));
    }
}
