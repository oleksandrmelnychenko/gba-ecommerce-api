using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SupplyOrderUkrainePaymentDeliveryProtocolRepository : ISupplyOrderUkrainePaymentDeliveryProtocolRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderUkrainePaymentDeliveryProtocolRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyOrderUkrainePaymentDeliveryProtocol protocol) {
        return _connection.Query<long>(
                "INSERT INTO [SupplyOrderUkrainePaymentDeliveryProtocol] " +
                "([Value], Discount, SupplyOrderUkrainePaymentDeliveryProtocolKeyId, UserId, SupplyOrderUkraineId, SupplyPaymentTaskId, Created, Updated, IsAccounting) " +
                "VALUES " +
                "(@Value, @Discount, @SupplyOrderUkrainePaymentDeliveryProtocolKeyId, @UserId, @SupplyOrderUkraineId, @SupplyPaymentTaskId, @Created, GETUTCDATE(), @IsAccounting); " +
                "SELECT SCOPE_IDENTITY()",
                protocol
            )
            .Single();
    }

    public void Add(IEnumerable<SupplyOrderUkrainePaymentDeliveryProtocol> protocols) {
        _connection.Execute(
            "INSERT INTO [SupplyOrderUkrainePaymentDeliveryProtocol] " +
            "([Value], Discount, SupplyOrderUkrainePaymentDeliveryProtocolKeyId, UserId, SupplyOrderUkraineId, SupplyPaymentTaskId, Created, Updated, IsAccounting) " +
            "VALUES " +
            "(@Value, @Discount, @SupplyOrderUkrainePaymentDeliveryProtocolKeyId, @UserId, @SupplyOrderUkraineId, @SupplyPaymentTaskId, @Created, GETUTCDATE(), @IsAccounting)",
            protocols
        );
    }

    public void Update(SupplyOrderUkrainePaymentDeliveryProtocol protocol) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkrainePaymentDeliveryProtocol] " +
            "SET [Value] = @Value, Discount = @Discount, SupplyOrderUkrainePaymentDeliveryProtocolKeyId = @SupplyOrderUkrainePaymentDeliveryProtocolKeyId, " +
            "UserId = @UserId, SupplyOrderUkraineId = @SupplyOrderUkraineId, SupplyPaymentTaskId = @SupplyPaymentTaskId, Created = @Created, Updated = GETUTCDATE(), " +
            "IsAccounting = @IsAccounting " +
            "WHERE ID = @Id",
            protocol
        );
    }

    public void Update(IEnumerable<SupplyOrderUkrainePaymentDeliveryProtocol> protocols) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkrainePaymentDeliveryProtocol] " +
            "SET [Value] = @Value, Discount = @Discount, SupplyOrderUkrainePaymentDeliveryProtocolKeyId = @SupplyOrderUkrainePaymentDeliveryProtocolKeyId, " +
            "UserId = @UserId, SupplyOrderUkraineId = @SupplyOrderUkraineId, SupplyPaymentTaskId = @SupplyPaymentTaskId, Created = @Created, Updated = GETUTCDATE(), " +
            "IsAccounting = @IsAccounting " +
            "WHERE ID = @Id",
            protocols
        );
    }

    public SupplyOrderUkrainePaymentDeliveryProtocol GetById(long id) {
        return _connection
            .Query<SupplyOrderUkrainePaymentDeliveryProtocol, SupplyOrderUkrainePaymentDeliveryProtocolKey, User, SupplyPaymentTask, SupplyOrderUkraine,
                SupplyOrderUkrainePaymentDeliveryProtocol>(
                "SELECT * " +
                "FROM [SupplyOrderUkrainePaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyOrderUkrainePaymentDeliveryProtocolKey] " +
                "ON [SupplyOrderUkrainePaymentDeliveryProtocolKey].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkrainePaymentDeliveryProtocolKeyID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkraineID " +
                "WHERE [SupplyOrderUkrainePaymentDeliveryProtocol].ID = @Id",
                (protocol, protocolKey, user, paymentTask, orderUkraine) => {
                    protocol.SupplyOrderUkrainePaymentDeliveryProtocolKey = protocolKey;
                    protocol.User = user;
                    protocol.SupplyPaymentTask = paymentTask;
                    protocol.SupplyOrderUkraine = orderUkraine;

                    return protocol;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public SupplyOrderUkrainePaymentDeliveryProtocol GetByNetId(Guid netId) {
        return _connection
            .Query<SupplyOrderUkrainePaymentDeliveryProtocol, SupplyOrderUkrainePaymentDeliveryProtocolKey, User, SupplyPaymentTask, SupplyOrderUkraine,
                SupplyOrderUkrainePaymentDeliveryProtocol>(
                "SELECT * " +
                "FROM [SupplyOrderUkrainePaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyOrderUkrainePaymentDeliveryProtocolKey] " +
                "ON [SupplyOrderUkrainePaymentDeliveryProtocolKey].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkrainePaymentDeliveryProtocolKeyID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkraineID " +
                "WHERE [SupplyOrderUkrainePaymentDeliveryProtocol].NetUID = @NetId",
                (protocol, protocolKey, user, paymentTask, orderUkraine) => {
                    protocol.SupplyOrderUkrainePaymentDeliveryProtocolKey = protocolKey;
                    protocol.User = user;
                    protocol.SupplyPaymentTask = paymentTask;
                    protocol.SupplyOrderUkraine = orderUkraine;

                    return protocol;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkrainePaymentDeliveryProtocol] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkrainePaymentDeliveryProtocol] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }
}