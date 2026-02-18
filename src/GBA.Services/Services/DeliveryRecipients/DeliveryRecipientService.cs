using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Repositories.Delivery.Contracts;
using GBA.Services.Services.DeliveryRecipients.Contracts;

namespace GBA.Services.Services.DeliveryRecipients;

public sealed class DeliveryRecipientService : IDeliveryRecipientService {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDeliveryRepositoriesFactory _deliveryRepositoriesFactory;

    public DeliveryRecipientService(
        IDeliveryRepositoriesFactory deliveryRepositoriesFactory,
        IDbConnectionFactory connectionFactory) {
        _deliveryRepositoriesFactory = deliveryRepositoriesFactory;

        _connectionFactory = connectionFactory;
    }

    public Task<List<DeliveryRecipient>> GetAllRecipientsByClientNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(
            _deliveryRepositoriesFactory
                .NewDeliveryRecipientRepository(connection)
                .GetAllRecipientsByClientNetId(netId)
        );
    }

    public Task<List<DeliveryRecipientAddress>> GetAllAddressesByRecipientNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(
            _deliveryRepositoriesFactory
                .NewDeliveryRecipientAddressRepository(connection)
                .GetAllByRecipientNetId(netId)
        );
    }
}
