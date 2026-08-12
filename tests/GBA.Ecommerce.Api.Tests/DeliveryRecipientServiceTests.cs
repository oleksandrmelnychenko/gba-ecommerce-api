using System.Data;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Delivery.Contracts;
using GBA.Services.Services.DeliveryRecipients;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class DeliveryRecipientServiceTests {
    [Fact]
    public async Task Invalid_recipient_is_rejected_before_any_database_access() {
        Mock<IDbConnectionFactory> connectionFactory = new();
        DeliveryRecipientService service = new(
            Mock.Of<IClientRepositoriesFactory>(),
            Mock.Of<IDeliveryRepositoriesFactory>(),
            connectionFactory.Object);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AddRecipient(Guid.NewGuid(), "   ", "0501112233"));

        connectionFactory.Verify(factory => factory.NewSqlConnection(), Times.Never);
    }

    [Fact]
    public async Task New_recipient_is_normalized_and_saved_for_the_authenticated_client() {
        Guid clientNetId = Guid.NewGuid();
        Client client = new() { Id = 42, NetUid = clientNetId };
        DeliveryRecipient created = new() {
            Id = 77,
            NetUid = Guid.NewGuid(),
            ClientId = client.Id,
            FullName = "New buyer",
            MobilePhone = "0509998877"
        };

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IClientRepository> clientRepository = new();
        clientRepository
            .Setup(repository => repository.GetByNetIdWithoutIncludes(clientNetId))
            .Returns(client);
        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(connection.Object))
            .Returns(clientRepository.Object);

        DeliveryRecipient inserted = null!;
        Mock<IDeliveryRecipientRepository> recipientRepository = new();
        recipientRepository
            .Setup(repository => repository.GetByClientIdAndContact(
                client.Id,
                "New buyer",
                "0509998877"))
            .Returns((DeliveryRecipient)null!);
        recipientRepository
            .Setup(repository => repository.Add(It.IsAny<DeliveryRecipient>()))
            .Callback<DeliveryRecipient>(recipient => inserted = recipient)
            .Returns(created.Id);
        recipientRepository
            .Setup(repository => repository.GetById(created.Id))
            .Returns(created);
        Mock<IDeliveryRepositoriesFactory> deliveryRepositoriesFactory = new();
        deliveryRepositoriesFactory
            .Setup(factory => factory.NewDeliveryRecipientRepository(connection.Object))
            .Returns(recipientRepository.Object);

        DeliveryRecipientService service = new(
            clientRepositoriesFactory.Object,
            deliveryRepositoriesFactory.Object,
            connectionFactory.Object);

        DeliveryRecipient result = await service.AddRecipient(
            clientNetId,
            "  New buyer  ",
            "  0509998877  ");

        Assert.Same(created, result);
        Assert.Equal(client.Id, inserted.ClientId);
        Assert.Equal("New buyer", inserted.FullName);
        Assert.Equal("0509998877", inserted.MobilePhone);
    }

    [Fact]
    public async Task Workplace_recipient_list_uses_the_main_client_identity() {
        Guid workplaceNetId = Guid.NewGuid();
        Client mainClient = new() { Id = 42, NetUid = Guid.NewGuid() };
        Workplace workplace = new() { MainClient = mainClient };
        List<DeliveryRecipient> recipients = [new DeliveryRecipient {
            Id = 77,
            NetUid = Guid.NewGuid(),
            ClientId = mainClient.Id,
            FullName = "Saved buyer"
        }];

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IClientRepository> clientRepository = new();
        clientRepository
            .Setup(repository => repository.GetByNetIdWithoutIncludes(workplaceNetId))
            .Returns((Client)null!);
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

        Mock<IDeliveryRecipientRepository> recipientRepository = new();
        recipientRepository
            .Setup(repository => repository.GetAllRecipientsByClientNetId(mainClient.NetUid))
            .Returns(recipients);
        Mock<IDeliveryRepositoriesFactory> deliveryRepositoriesFactory = new();
        deliveryRepositoriesFactory
            .Setup(factory => factory.NewDeliveryRecipientRepository(connection.Object))
            .Returns(recipientRepository.Object);

        DeliveryRecipientService service = new(
            clientRepositoriesFactory.Object,
            deliveryRepositoriesFactory.Object,
            connectionFactory.Object);

        List<DeliveryRecipient> result = await service.GetAllRecipientsByClientNetId(workplaceNetId);

        Assert.Same(recipients, result);
    }

    [Fact]
    public async Task Invalid_address_is_rejected_before_any_database_access() {
        Mock<IDbConnectionFactory> connectionFactory = new();
        DeliveryRecipientService service = new(
            Mock.Of<IClientRepositoriesFactory>(),
            Mock.Of<IDeliveryRepositoriesFactory>(),
            connectionFactory.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAddress(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "   ",
            "Kyiv",
            string.Empty));

        connectionFactory.Verify(factory => factory.NewSqlConnection(), Times.Never);
    }

    [Fact]
    public async Task New_address_is_normalized_and_saved_for_the_authenticated_clients_recipient() {
        Guid clientNetId = Guid.NewGuid();
        Guid recipientNetId = Guid.NewGuid();
        Client client = new() { Id = 42, NetUid = clientNetId };
        DeliveryRecipient recipient = new() {
            Id = 77,
            NetUid = recipientNetId,
            ClientId = client.Id
        };
        DeliveryRecipientAddress created = new() {
            Id = 88,
            NetUid = Guid.NewGuid(),
            DeliveryRecipientId = recipient.Id,
            Value = "Peremohy Avenue 10",
            City = "Kyiv",
            Department = "Warehouse 3"
        };

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewSqlConnection()).Returns(connection.Object);

        Mock<IClientRepository> clientRepository = new();
        clientRepository.Setup(repository => repository.GetByNetIdWithoutIncludes(clientNetId)).Returns(client);
        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(connection.Object))
            .Returns(clientRepository.Object);

        Mock<IDeliveryRecipientRepository> recipientRepository = new();
        recipientRepository.Setup(repository => repository.GetByNetId(recipientNetId)).Returns(recipient);
        DeliveryRecipientAddress inserted = null!;
        Mock<IDeliveryRecipientAddressRepository> addressRepository = new();
        addressRepository
            .Setup(repository => repository.GetAllByRecipientNetId(recipientNetId))
            .Returns([]);
        addressRepository
            .Setup(repository => repository.Add(It.IsAny<DeliveryRecipientAddress>()))
            .Callback<DeliveryRecipientAddress>(address => inserted = address)
            .Returns(created.Id);
        addressRepository.Setup(repository => repository.GetById(created.Id)).Returns(created);
        Mock<IDeliveryRepositoriesFactory> deliveryRepositoriesFactory = new();
        deliveryRepositoriesFactory
            .Setup(factory => factory.NewDeliveryRecipientRepository(connection.Object))
            .Returns(recipientRepository.Object);
        deliveryRepositoriesFactory
            .Setup(factory => factory.NewDeliveryRecipientAddressRepository(connection.Object))
            .Returns(addressRepository.Object);

        DeliveryRecipientService service = new(
            clientRepositoriesFactory.Object,
            deliveryRepositoriesFactory.Object,
            connectionFactory.Object);

        DeliveryRecipientAddress result = await service.AddAddress(
            clientNetId,
            recipientNetId,
            "  Peremohy Avenue 10  ",
            "  Kyiv  ",
            "  Warehouse 3  ");

        Assert.Same(created, result);
        Assert.Equal(recipient.Id, inserted.DeliveryRecipientId);
        Assert.Equal("Peremohy Avenue 10", inserted.Value);
        Assert.Equal("Kyiv", inserted.City);
        Assert.Equal("Warehouse 3", inserted.Department);
    }

    [Fact]
    public async Task Address_for_another_clients_recipient_is_rejected_without_writing() {
        Guid clientNetId = Guid.NewGuid();
        Guid recipientNetId = Guid.NewGuid();
        Client client = new() { Id = 42, NetUid = clientNetId };
        DeliveryRecipient foreignRecipient = new() {
            Id = 77,
            NetUid = recipientNetId,
            ClientId = 999
        };

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewSqlConnection()).Returns(connection.Object);
        Mock<IClientRepository> clientRepository = new();
        clientRepository.Setup(repository => repository.GetByNetIdWithoutIncludes(clientNetId)).Returns(client);
        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(connection.Object))
            .Returns(clientRepository.Object);
        Mock<IDeliveryRecipientRepository> recipientRepository = new();
        recipientRepository.Setup(repository => repository.GetByNetId(recipientNetId)).Returns(foreignRecipient);
        Mock<IDeliveryRecipientAddressRepository> addressRepository = new();
        Mock<IDeliveryRepositoriesFactory> deliveryRepositoriesFactory = new();
        deliveryRepositoriesFactory
            .Setup(factory => factory.NewDeliveryRecipientRepository(connection.Object))
            .Returns(recipientRepository.Object);
        deliveryRepositoriesFactory
            .Setup(factory => factory.NewDeliveryRecipientAddressRepository(connection.Object))
            .Returns(addressRepository.Object);

        DeliveryRecipientService service = new(
            clientRepositoriesFactory.Object,
            deliveryRepositoriesFactory.Object,
            connectionFactory.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAddress(
            clientNetId,
            recipientNetId,
            "Peremohy Avenue 10",
            "Kyiv",
            string.Empty));

        addressRepository.Verify(
            repository => repository.Add(It.IsAny<DeliveryRecipientAddress>()),
            Times.Never);
    }
}
