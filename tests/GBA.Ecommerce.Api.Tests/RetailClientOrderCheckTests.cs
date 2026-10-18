using System.Data;
using System.Text.Json;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;
using GBA.Domain.Repositories.Ecommerce.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Clients;
using GBA.Services.Services.Orders.Contracts;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class RetailClientOrderCheckTests {
    [Fact]
    public async Task Unknown_retail_client_requests_report_a_controlled_request_error() {
        Guid unknownNetId = Guid.NewGuid();
        Mock<IDbConnection> connection = new();
        Mock<IRetailClientRepository> retailClientRepository = new();
        retailClientRepository
            .Setup(repository => repository.GetByNetId(unknownNetId))
            .Returns((RetailClient)null!);

        Mock<IRetailClientRepositoriesFactory> retailClientRepositoriesFactory = new();
        retailClientRepositoriesFactory
            .Setup(factory => factory.NewRetailClientRepository(connection.Object))
            .Returns(retailClientRepository.Object);

        Mock<IProductAvailabilityRepository> productAvailabilityRepository = new();
        Mock<IProductRepositoriesFactory> productRepositoriesFactory = new();
        productRepositoriesFactory
            .Setup(factory => factory.NewProductAvailabilityRepository(connection.Object))
            .Returns(productAvailabilityRepository.Object);

        Mock<IStorageRepository> storageRepository = new();
        storageRepository
            .Setup(repository => repository.GetWithHighestPriority(null))
            .Returns(new Storage { Id = 7 });
        Mock<IStorageRepositoryFactory> storageRepositoryFactory = new();
        storageRepositoryFactory
            .Setup(factory => factory.NewStorageRepository(connection.Object))
            .Returns(storageRepository.Object);

        ClientService service = CreateService(
            connection,
            retailClientRepositoriesFactory,
            productRepositoriesFactory,
            storageRepositoryFactory);

        ArgumentException getException = await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetRetailClientByNetId(unknownNetId));
        ArgumentException checkException = await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetRetailClientByNetIdCheckOrderItems(unknownNetId));

        Assert.Equal("netId", getException.ParamName);
        Assert.Equal("netId", checkException.ParamName);
        retailClientRepository.Verify(repository => repository.Update(It.IsAny<RetailClient>()), Times.Never);
        productAvailabilityRepository.Verify(
            repository => repository.GetByProductAndStorageIds(It.IsAny<long>(), It.IsAny<long>()),
            Times.Never);
    }

    [Fact]
    public async Task Known_retail_client_keeps_available_items_and_reports_unavailable_items() {
        Guid clientNetId = Guid.NewGuid();
        RetailClient client = new() {
            NetUid = clientNetId,
            ShoppingCartJson = """
                [
                  { "Id": 0, "Qty": 2, "Product": { "Id": 11, "VendorCode": "AVAILABLE" } },
                  { "Id": 0, "Qty": 1, "Product": { "Id": 12, "VendorCode": "MISSING" } }
                ]
                """
        };

        Mock<IDbConnection> connection = new();
        Mock<IRetailClientRepository> retailClientRepository = new();
        retailClientRepository
            .Setup(repository => repository.GetByNetId(clientNetId))
            .Returns(client);

        Mock<IRetailClientRepositoriesFactory> retailClientRepositoriesFactory = new();
        retailClientRepositoriesFactory
            .Setup(factory => factory.NewRetailClientRepository(connection.Object))
            .Returns(retailClientRepository.Object);

        Mock<IProductAvailabilityRepository> productAvailabilityRepository = new();
        productAvailabilityRepository
            .Setup(repository => repository.GetByProductAndStorageIds(11, 7))
            .Returns(new ProductAvailability { Amount = 3 });
        productAvailabilityRepository
            .Setup(repository => repository.GetByProductAndStorageIds(12, 7))
            .Returns((ProductAvailability)null!);
        Mock<IProductRepositoriesFactory> productRepositoriesFactory = new();
        productRepositoriesFactory
            .Setup(factory => factory.NewProductAvailabilityRepository(connection.Object))
            .Returns(productAvailabilityRepository.Object);

        Mock<IStorageRepository> storageRepository = new();
        storageRepository
            .Setup(repository => repository.GetWithHighestPriority(null))
            .Returns(new Storage { Id = 7 });
        Mock<IStorageRepositoryFactory> storageRepositoryFactory = new();
        storageRepositoryFactory
            .Setup(factory => factory.NewStorageRepository(connection.Object))
            .Returns(storageRepository.Object);

        ClientService service = CreateService(
            connection,
            retailClientRepositoriesFactory,
            productRepositoriesFactory,
            storageRepositoryFactory);

        (RetailClient resultClient, string unavailableInfo) =
            await service.GetRetailClientByNetIdCheckOrderItems(clientNetId);

        List<OrderItem> remainingItems =
            JsonSerializer.Deserialize<List<OrderItem>>(resultClient.ShoppingCartJson)!;
        OrderItem remainingItem = Assert.Single(remainingItems);
        Assert.Equal(11, remainingItem.Product.Id);
        Assert.Contains("MISSING", unavailableInfo, StringComparison.Ordinal);
        retailClientRepository.Verify(repository => repository.Update(client), Times.Once);
    }

    private static ClientService CreateService(
        Mock<IDbConnection> connection,
        Mock<IRetailClientRepositoriesFactory> retailClientRepositoriesFactory,
        Mock<IProductRepositoriesFactory> productRepositoriesFactory,
        Mock<IStorageRepositoryFactory> storageRepositoryFactory) {
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        return new ClientService(
            Mock.Of<IClientRepositoriesFactory>(),
            Mock.Of<IExchangeRateRepositoriesFactory>(),
            retailClientRepositoriesFactory.Object,
            Mock.Of<IEcommerceAdminPanelRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IOrderService>(),
            productRepositoriesFactory.Object,
            storageRepositoryFactory.Object);
    }
}
