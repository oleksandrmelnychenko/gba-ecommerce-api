using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Repositories.Delivery.Contracts;

namespace GBA.Domain.Repositories.Delivery;

public sealed class DeliveryRecipientAddressRepository : IDeliveryRecipientAddressRepository {
    private readonly IDbConnection _connection;

    public DeliveryRecipientAddressRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(DeliveryRecipientAddress deliveryAddress) {
        return _connection.Query<long>(
                "INSERT INTO DeliveryRecipientAddress (DeliveryRecipientId, Value, Department, City, Updated) " +
                "VALUES (@DeliveryRecipientId, @Value, @Department, @City, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                deliveryAddress
            )
            .Single();
    }

    public List<DeliveryRecipientAddress> GetAllByRecipientNetId(Guid recipientNetId) {
        return _connection.Query<DeliveryRecipientAddress, DeliveryRecipient, DeliveryRecipientAddress>(
                "SELECT * " +
                "FROM [DeliveryRecipientAddress] " +
                "LEFT JOIN [DeliveryRecipient] " +
                "ON [DeliveryRecipient].ID = [DeliveryRecipientAddress].DeliveryRecipientID " +
                "AND [DeliveryRecipient].Deleted = 0 " +
                "WHERE [DeliveryRecipient].NetUID = @RecipientNetId " +
                "ORDER BY [DeliveryRecipientAddress].Priority DESC, [DeliveryRecipientAddress].Value, [DeliveryRecipient].Priority DESC, [DeliveryRecipient].FullName",
                (deliveryRecipientAddress, deliveryRecipient) => {
                    deliveryRecipientAddress.DeliveryRecipient = deliveryRecipient;

                    return deliveryRecipientAddress;
                },
                new { RecipientNetId = recipientNetId }
            )
            .ToList();
    }

    public DeliveryRecipientAddress GetById(long id) {
        return _connection.Query<DeliveryRecipientAddress>(
                "SELECT * FROM DeliveryRecipientAddress WHERE Id = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public DeliveryRecipientAddress GetByNetId(Guid netId) {
        return _connection.Query<DeliveryRecipientAddress>(
                "SELECT * FROM DeliveryRecipientAddress WHERE NetUid = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void IncreasePriority(long id) {
        _connection.Execute(
            "UPDATE DeliveryRecipientAddress SET " +
            "Updated = getutcdate(), Priority = Priority + 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void DecreasePriority(long id) {
        _connection.Execute(
            "UPDATE DeliveryRecipientAddress SET " +
            "Updated = getutcdate(), Priority = Priority - 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }


    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE DeliveryRecipientAddress SET Deleted = 1 WHERE NetUid = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(DeliveryRecipientAddress deliveryAddress) {
        _connection.Execute(
            "UPDATE DeliveryRecipientAddress " +
            "SET Value = @Value, Department = @Department, City = @City, Updated = getutcdate() " +
            "WHERE NetUid = @NetUid",
            deliveryAddress
        );
    }
}