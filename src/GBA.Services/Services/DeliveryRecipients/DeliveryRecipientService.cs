using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Delivery.Contracts;
using GBA.Services.Services.DeliveryRecipients.Contracts;

namespace GBA.Services.Services.DeliveryRecipients;

public sealed class DeliveryRecipientService : IDeliveryRecipientService {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDeliveryRepositoriesFactory _deliveryRepositoriesFactory;

    public DeliveryRecipientService(
        IClientRepositoriesFactory clientRepositoriesFactory,
        IDeliveryRepositoriesFactory deliveryRepositoriesFactory,
        IDbConnectionFactory connectionFactory) {
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _deliveryRepositoriesFactory = deliveryRepositoriesFactory;

        _connectionFactory = connectionFactory;
    }

    public Task<List<DeliveryRecipient>> GetAllRecipientsByClientNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Client client = ResolveClient(connection, netId);
        return Task.FromResult(
            _deliveryRepositoriesFactory
                .NewDeliveryRecipientRepository(connection)
                .GetAllRecipientsByClientNetId(client.NetUid)
        );
    }

    public Task<DeliveryRecipient> AddRecipient(Guid actorNetId, string fullName, string mobilePhone) {
        string normalizedFullName = fullName?.Trim() ?? string.Empty;
        string normalizedMobilePhone = mobilePhone?.Trim() ?? string.Empty;
        if (normalizedFullName.Length is < 1 or > 250)
            throw new ArgumentException("Delivery recipient name is invalid.", nameof(fullName));
        if (normalizedMobilePhone.Length is < 1 or > 100)
            throw new ArgumentException("Delivery recipient phone is invalid.", nameof(mobilePhone));
        if (actorNetId == Guid.Empty)
            throw new ArgumentException("A client profile could not be resolved.", nameof(actorNetId));

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Client client = ResolveClient(connection, actorNetId);
        IDeliveryRecipientRepository repository =
            _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection);
        DeliveryRecipient existing = repository.GetByClientIdAndContact(
            client.Id,
            normalizedFullName,
            normalizedMobilePhone);
        if (existing != null) return Task.FromResult(existing);

        DeliveryRecipient recipient = new() {
            ClientId = client.Id,
            FullName = normalizedFullName,
            MobilePhone = normalizedMobilePhone
        };
        recipient.Id = repository.Add(recipient);
        DeliveryRecipient created = repository.GetById(recipient.Id);
        if (created == null || created.NetUid == Guid.Empty)
            throw new InvalidOperationException("The delivery recipient could not be created.");

        return Task.FromResult(created);
    }

    public Task<List<DeliveryRecipientAddress>> GetAllAddressesByRecipientNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(
            _deliveryRepositoriesFactory
                .NewDeliveryRecipientAddressRepository(connection)
                .GetAllByRecipientNetId(netId)
        );
    }

    private Client ResolveClient(IDbConnection connection, Guid actorNetId) {
        if (actorNetId == Guid.Empty)
            throw new ArgumentException("A client profile could not be resolved.", nameof(actorNetId));

        Client client = _clientRepositoriesFactory
            .NewClientRepository(connection)
            .GetByNetIdWithoutIncludes(actorNetId);
        if (client != null) return client;

        client = _clientRepositoriesFactory
            .NewWorkplaceRepository(connection)
            .GetByNetIdWithClient(actorNetId)
            ?.MainClient;
        return client ?? throw new ArgumentException(
            "A client profile could not be resolved.",
            nameof(actorNetId));
    }
}
