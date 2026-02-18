using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Protocols;

public sealed class SupplyOrderPaymentDeliveryProtocolRepository : ISupplyOrderPaymentDeliveryProtocolRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderPaymentDeliveryProtocolRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyOrderPaymentDeliveryProtocol protocol) {
        return _connection.Query<long>(
                "INSERT INTO SupplyOrderPaymentDeliveryProtocol (SupplyPaymentTaskID, UserID, SupplyInvoiceID, SupplyOrderPaymentDeliveryProtocolKeyID, [Value], SupplyProFormID, Discount, Updated, IsAccounting) " +
                "VALUES(@SupplyPaymentTaskID, @UserID, @SupplyInvoiceID, @SupplyOrderPaymentDeliveryProtocolKeyID, @Value, @SupplyProFormID, @Discount, getutcdate(), @IsAccounting); " +
                "SELECT SCOPE_IDENTITY()",
                protocol
            )
            .Single();
    }

    public void Add(IEnumerable<SupplyOrderPaymentDeliveryProtocol> protocols) {
        _connection.Execute(
            "INSERT INTO SupplyOrderPaymentDeliveryProtocol " +
            "(SupplyPaymentTaskID, UserID, SupplyInvoiceID, SupplyOrderPaymentDeliveryProtocolKeyId, [Value], SupplyProFormID, Discount, Updated, IsAccounting) " +
            "VALUES " +
            "(@SupplyPaymentTaskID, @UserID, @SupplyInvoiceID, @SupplyOrderPaymentDeliveryProtocolKeyID, @Value, @SupplyProFormID, @Discount, getutcdate(), @IsAccounting)",
            protocols
        );
    }

    public void Update(SupplyOrderPaymentDeliveryProtocol protocol) {
        _connection.Execute(
            "INSERT INTO [SupplyOrderPaymentDeliveryProtocol] " +
            "SupplyPaymentTaskID = @SupplyPaymentTaskID, [Value] = @Value, Discount = @Discount, Updated = GETUTCDATE(), IsAccounting = @IsAccounting " +
            "WHERE ID = @Id",
            protocol
        );
    }

    public void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId) {
        _connection.Execute(
            "UPDATE [SupplyOrderPaymentDeliveryProtocol] " +
            "SET SupplyInvoiceID = @ToInvoiceId, Updated = GETUTCDATE() " +
            "WHERE SupplyInvoiceID = @FromInvoiceId",
            new { FromInvoiceId = fromInvoiceId, ToInvoiceId = toInvoiceId }
        );
    }

    public List<SupplyOrderPaymentDeliveryProtocol> GetAllByTaskIds(IEnumerable<long> ids) {
        return _connection
            .Query<SupplyOrderPaymentDeliveryProtocol, User, SupplyPaymentTask, SupplyInvoice, SupplyOrderPaymentDeliveryProtocolKey, SupplyOrderPaymentDeliveryProtocol>(
                "SELECT * FROM SupplyOrderPaymentDeliveryProtocol " +
                "LEFT OUTER JOIN [User] " +
                "ON [User].ID = SupplyOrderPaymentDeliveryProtocol.UserID " +
                "LEFT OUTER JOIN SupplyPaymentTask " +
                "ON SupplyOrderPaymentDeliveryProtocol.SupplyPaymentTaskID = SupplyPaymentTask.ID " +
                "AND SupplyPaymentTask.Deleted = 0 " +
                "LEFT OUTER JOIN SupplyInvoice " +
                "ON SupplyOrderPaymentDeliveryProtocol.SupplyInvoiceID = SupplyInvoice.ID " +
                "AND SupplyInvoice.Deleted = 0 " +
                "LEFT JOIN SupplyOrderPaymentDeliveryProtocolKey " +
                "ON SupplyOrderPaymentDeliveryProtocolKey.ID = SupplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKeyID " +
                "WHERE SupplyOrderPaymentDeliveryProtocol.SupplyPaymentTaskID IN @Ids",
                (protocol, user, task, invoice, key) => {
                    if (user != null) protocol.User = user;

                    if (task != null) protocol.SupplyPaymentTask = task;

                    if (key != null) protocol.SupplyOrderPaymentDeliveryProtocolKey = key;

                    if (invoice != null) protocol.SupplyInvoice = invoice;

                    return protocol;
                },
                new { Ids = ids }
            )
            .ToList();
    }

    public SupplyOrderPaymentDeliveryProtocol GetById(long id) {
        return _connection.Query<SupplyOrderPaymentDeliveryProtocol, User, SupplyPaymentTask, SupplyInvoice, SupplyOrderPaymentDeliveryProtocol>(
                "SELECT * FROM SupplyOrderPaymentDeliveryProtocol " +
                "LEFT OUTER JOIN [User] " +
                "ON [User].ID = SupplyOrderPaymentDeliveryProtocol.UserID " +
                "LEFT OUTER JOIN SupplyPaymentTask " +
                "ON SupplyOrderPaymentDeliveryProtocol.SupplyPaymentTaskID = SupplyPaymentTask.ID " +
                "AND SupplyPaymentTask.Deleted = 0 " +
                "LEFT OUTER JOIN SupplyInvoice " +
                "ON SupplyOrderPaymentDeliveryProtocol.SupplyInvoiceID = SupplyInvoice.ID " +
                "AND SupplyInvoice.Deleted = 0 " +
                "WHERE SupplyOrderPaymentDeliveryProtocol.ID = @Id",
                (protocol, user, task, invoice) => {
                    if (user != null) protocol.User = user;

                    if (task != null) protocol.SupplyPaymentTask = task;

                    if (invoice != null) protocol.SupplyInvoice = invoice;

                    return protocol;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public SupplyOrderPaymentDeliveryProtocol GetByNetId(Guid netId) {
        return _connection.Query<SupplyOrderPaymentDeliveryProtocol, User, SupplyPaymentTask, SupplyInvoice, SupplyOrderPaymentDeliveryProtocol>(
                "SELECT * FROM SupplyOrderPaymentDeliveryProtocol " +
                "LEFT OUTER JOIN [User] " +
                "ON [User].ID = SupplyOrderPaymentDeliveryProtocol.UserID " +
                "LEFT OUTER JOIN SupplyPaymentTask " +
                "ON SupplyOrderPaymentDeliveryProtocol.SupplyPaymentTaskID = SupplyPaymentTask.ID " +
                "AND SupplyPaymentTask.Deleted = 0 " +
                "LEFT OUTER JOIN SupplyInvoice " +
                "ON SupplyOrderPaymentDeliveryProtocol.SupplyInvoiceID = SupplyInvoice.ID " +
                "AND SupplyInvoice.Deleted = 0 " +
                "WHERE SupplyOrderPaymentDeliveryProtocol.NetUID = @NetId",
                (protocol, user, task, invoice) => {
                    if (user != null) protocol.User = user;

                    if (task != null) protocol.SupplyPaymentTask = task;

                    if (invoice != null) protocol.SupplyInvoice = invoice;

                    return protocol;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE SupplyOrderPaymentDeliveryProtocol SET Deleted = 1 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(IEnumerable<SupplyOrderPaymentDeliveryProtocol> protocols) {
        _connection.Execute(
            "UPDATE SupplyOrderPaymentDeliveryProtocol " +
            "SET SupplyPaymentTaskID = @SupplyPaymentTaskID, UserID = @UserID, " +
            "SupplyInvoiceID = @SupplyInvoiceID, SupplyOrderPaymentDeliveryProtocolKeyID = @SupplyOrderPaymentDeliveryProtocolKeyID, " +
            "[Value] = @Value, SupplyProFormID = @SupplyProFormID, Discount = @Discount, Updated = getutcdate(), " +
            "IsAccounting = @IsAccounting " +
            "WHERE NetUID = @NetUID",
            protocols
        );
    }
}