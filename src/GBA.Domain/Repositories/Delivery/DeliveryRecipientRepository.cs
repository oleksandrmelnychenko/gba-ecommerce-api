using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Repositories.Delivery.Contracts;

namespace GBA.Domain.Repositories.Delivery;

public sealed class DeliveryRecipientRepository : IDeliveryRecipientRepository {
    private readonly IDbConnection _connection;

    public DeliveryRecipientRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(DeliveryRecipient deliveryRecipient) {
        return _connection.Query<long>(
                "INSERT INTO DeliveryRecipient (ClientId, FullName, MobilePhone, Updated) " +
                "VALUES (@ClientId, @FullName, @MobilePhone, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                deliveryRecipient
            )
            .Single();
    }

    public List<DeliveryRecipient> GetAll() {
        return _connection.Query<DeliveryRecipient>(
                "SELECT * FROM DeliveryRecipient " +
                "WHERE Deleted = 0 " +
                "ORDER BY DeliveryRecipient.Priority DESC, DeliveryRecipient.FullName"
            )
            .ToList();
    }

    public List<DeliveryRecipient> GetAllRecipientsByClientNetId(Guid clientNetId) {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, DeliveryRecipient> recipientDict = new();

        _connection.Query<DeliveryRecipient, DeliveryRecipientAddress, DeliveryRecipient>(
            "SELECT [DeliveryRecipient].* " +
            ",[DeliveryRecipientAddress].* " +
            "FROM [DeliveryRecipient] " +
            "LEFT JOIN [DeliveryRecipientAddress] " +
            "ON [DeliveryRecipientAddress].DeliveryRecipientID = [DeliveryRecipient].ID " +
            "AND [DeliveryRecipientAddress].Deleted = 0 " +
            "LEFT JOIN [Client] " +
            "ON [DeliveryRecipient].ClientID = [Client].ID " +
            "WHERE [Client].NetUID = @ClientNetId " +
            "AND [DeliveryRecipient].Deleted = 0 " +
            "ORDER BY [DeliveryRecipient].Priority DESC, [DeliveryRecipient].FullName, [DeliveryRecipientAddress].Priority DESC, [DeliveryRecipientAddress].Value",
            (deliveryRecipient, deliveryRecipientAddress) => {
                // O(1) lookup with TryGetValue instead of O(n) Any + First
                if (recipientDict.TryGetValue(deliveryRecipient.Id, out DeliveryRecipient existingRecipient)) {
                    if (deliveryRecipientAddress != null)
                        existingRecipient.DeliveryRecipientAddresses.Add(deliveryRecipientAddress);
                } else {
                    if (deliveryRecipientAddress != null) deliveryRecipient.DeliveryRecipientAddresses.Add(deliveryRecipientAddress);

                    recipientDict[deliveryRecipient.Id] = deliveryRecipient;
                }

                return deliveryRecipient;
            },
            new { ClientNetId = clientNetId }
        );

        return recipientDict.Values.ToList();
    }

    public List<DeliveryRecipient> GetAllRecipientsDeletedByClientNetId(Guid clientNetId) {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, DeliveryRecipient> recipientDict = new();

        _connection.Query<DeliveryRecipient, DeliveryRecipientAddress, DeliveryRecipient>(
            "SELECT [DeliveryRecipient].* " +
            ",[DeliveryRecipientAddress].* " +
            "FROM [DeliveryRecipient] " +
            "LEFT JOIN [DeliveryRecipientAddress] " +
            "ON [DeliveryRecipientAddress].DeliveryRecipientID = [DeliveryRecipient].ID " +
            "AND [DeliveryRecipientAddress].Deleted = 0 " +
            "LEFT JOIN [Client] " +
            "ON [DeliveryRecipient].ClientID = [Client].ID " +
            "WHERE [Client].NetUID = @ClientNetId " +
            "ORDER BY [DeliveryRecipient].Priority DESC, [DeliveryRecipient].FullName, [DeliveryRecipientAddress].Priority DESC, [DeliveryRecipientAddress].Value",
            (deliveryRecipient, deliveryRecipientAddress) => {
                // O(1) lookup with TryGetValue instead of O(n) Any + First
                if (recipientDict.TryGetValue(deliveryRecipient.Id, out DeliveryRecipient existingRecipient)) {
                    if (deliveryRecipientAddress != null)
                        existingRecipient.DeliveryRecipientAddresses.Add(deliveryRecipientAddress);
                } else {
                    if (deliveryRecipientAddress != null) deliveryRecipient.DeliveryRecipientAddresses.Add(deliveryRecipientAddress);

                    recipientDict[deliveryRecipient.Id] = deliveryRecipient;
                }

                return deliveryRecipient;
            },
            new { ClientNetId = clientNetId }
        );

        return recipientDict.Values.ToList();
    }

    public void IncreasePriority(long id) {
        _connection.Execute(
            "UPDATE DeliveryRecipient SET " +
            "Updated = getutcdate(), Priority = Priority + 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void DecreasePriority(long id) {
        _connection.Execute(
            "UPDATE DeliveryRecipient SET " +
            "Updated = getutcdate(), Priority = Priority - 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public DeliveryRecipient GetById(long id) {
        return _connection.Query<DeliveryRecipient>(
                "SELECT * FROM DeliveryRecipient WHERE Id = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public DeliveryRecipient GetByNetId(Guid netId) {
        return _connection.Query<DeliveryRecipient>(
                "SELECT * FROM DeliveryRecipient WHERE NetUid = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE DeliveryRecipient SET Deleted = 1 WHERE NetUid = @NetId",
            new { NetId = netId }
        );
    }

    public void ReturnRemove(Guid netId) {
        _connection.Execute(
            "UPDATE DeliveryRecipient SET Deleted = 0 WHERE NetUid = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(DeliveryRecipient deliveryRecipient) {
        _connection.Execute(
            "UPDATE DeliveryRecipient " +
            "SET ClientId = @ClientId, FullName = @FullName, MobilePhone = @MobilePhone, Updated = getutcdate() " +
            "WHERE NetUid = @NetUid",
            deliveryRecipient
        );
    }
}