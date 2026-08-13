using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Repositories.Delivery.Contracts;

namespace GBA.Domain.Repositories.Delivery;

public sealed class DeliveryRecipientAddressRepository : IDeliveryRecipientAddressRepository {
    private const int AddressMutationLockTimeoutMilliseconds = 30000;

    private readonly IDbConnection _connection;

    public DeliveryRecipientAddressRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void AcquireAddressMutationLock(Guid recipientNetId) {
        if (recipientNetId == Guid.Empty)
            throw new ArgumentException(
                "The delivery recipient identity is invalid.",
                nameof(recipientNetId));

        int lockResult = _connection.QuerySingle<int>(
            "DECLARE @LockResult int; " +
            "EXEC @LockResult = sys.sp_getapplock " +
            "@Resource = @Resource, " +
            "@LockMode = N'Exclusive', " +
            "@LockOwner = N'Transaction', " +
            "@LockTimeout = @LockTimeoutMilliseconds, " +
            "@DbPrincipal = N'public'; " +
            "SELECT @LockResult;",
            new {
                Resource = $"GBA_ECOM_DELIVERY_ADDRESS_{recipientNetId:N}",
                LockTimeoutMilliseconds =
                    AddressMutationLockTimeoutMilliseconds
            });

        if (lockResult < 0)
            throw new TimeoutException(
                $"The delivery address mutation lock could not be acquired ({lockResult}).");
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
                "AND [DeliveryRecipientAddress].Deleted = 0 " +
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
