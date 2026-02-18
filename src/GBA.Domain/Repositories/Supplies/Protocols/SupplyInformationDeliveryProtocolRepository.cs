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

public sealed class SupplyInformationDeliveryProtocolRepository : ISupplyInformationDeliveryProtocolRepository {
    private readonly IDbConnection _connection;

    public SupplyInformationDeliveryProtocolRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyInformationDeliveryProtocol protocol) {
        return _connection.Query<long>(
                "INSERT INTO SupplyInformationDeliveryProtocol " +
                "(SupplyProFormID, SupplyOrderID, UserID, SupplyInvoiceID, SupplyInformationDeliveryProtocolKeyID, [Value], IsDefault, Updated) " +
                "VALUES " +
                "(@SupplyProFormID, @SupplyOrderID, @UserID, @SupplyInvoiceID, @SupplyInformationDeliveryProtocolKeyID, @Value, @IsDefault, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                protocol
            )
            .Single();
    }

    public void Add(IEnumerable<SupplyInformationDeliveryProtocol> protocols) {
        _connection.Execute(
            "INSERT INTO SupplyInformationDeliveryProtocol " +
            "(SupplyProFormID, SupplyOrderID, UserID, SupplyInvoiceID, SupplyInformationDeliveryProtocolKeyID, [Value], IsDefault, Updated, Created) " +
            "VALUES " +
            "(@SupplyProFormID, @SupplyOrderID, @UserID, @SupplyInvoiceID, @SupplyInformationDeliveryProtocolKeyID, @Value, @IsDefault, getutcdate(), @Created)",
            protocols
        );
    }

    public SupplyInformationDeliveryProtocol GetById(long id) {
        return _connection.Query<SupplyInformationDeliveryProtocol, SupplyInformationDeliveryProtocolKey,
                SupplyInvoice, SupplyOrder, User, SupplyInformationDeliveryProtocol>(
                "SELECT * FROM SupplyInformationDeliveryProtocol " +
                "LEFT OUTER JOIN SupplyOrderInformationDeliveryProtocolKey " +
                "ON SupplyOrderInformationDeliveryProtocolKey.ID = SupplyInformationDeliveryProtocol.SupplyInformationDeliveryProtocolKeyID " +
                "AND SupplyOrderInformationDeliveryProtocolKey.Deleted = 0 " +
                "LEFT OUTER JOIN SupplyInvoice " +
                "ON SupplyInvoice.ID = SupplyInformationDeliveryProtocol.SupplyInvoiceID " +
                "AND SupplyInvoice.Deleted = 0 " +
                "LEFT OUTER JOIN SupplyOrder " +
                "ON SupplyOrder.ID = SupplyInformationDeliveryProtocol.SupplyOrderID " +
                "AND SupplyOrder.Deleted = 0 " +
                "LEFT OUTER JOIN [User] " +
                "ON [User].ID = SupplyInformationDeliveryProtocol.UserID " +
                "WHERE SupplyInformationDeliveryProtocol.ID = @Id",
                (protocol, key, invoice, order, user) => {
                    if (key != null) protocol.SupplyInformationDeliveryProtocolKey = key;

                    if (invoice != null) protocol.SupplyInvoice = invoice;

                    if (order != null) protocol.SupplyOrder = order;

                    if (user != null) protocol.User = user;

                    return protocol;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public SupplyInformationDeliveryProtocol GetDefaultProtocolBySupplyOrderNetId(Guid netId) {
        return _connection.Query<SupplyInformationDeliveryProtocol, SupplyInformationDeliveryProtocolKey, SupplyInformationDeliveryProtocol>(
                "SELECT SupplyInformationDeliveryProtocol.*, " +
                "SupplyInformationDeliveryProtocolKey.* " +
                "FROM SupplyInformationDeliveryProtocol " +
                "LEFT JOIN SupplyInformationDeliveryProtocolKey " +
                "ON SupplyInformationDeliveryProtocol.SupplyInformationDeliveryProtocolKeyID = SupplyInformationDeliveryProtocolKey.ID " +
                "LEFT JOIN SupplyOrder " +
                "ON SupplyInformationDeliveryProtocol.SupplyOrderID = SupplyOrder.ID " +
                "WHERE SupplyOrder.NetUID = @NetId " +
                "AND SupplyInformationDeliveryProtocol.SupplyInvoiceID IS NULL " +
                "AND SupplyInformationDeliveryProtocol.SupplyProFormID IS NULL " +
                "AND SupplyInformationDeliveryProtocol.IsDefault = 1",
                (protocol, key) => {
                    protocol.SupplyInformationDeliveryProtocolKey = key;

                    return protocol;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public SupplyInformationDeliveryProtocol GetByNetId(Guid netId) {
        return _connection.Query<SupplyInformationDeliveryProtocol, SupplyInformationDeliveryProtocolKey,
                SupplyInvoice, SupplyOrder, User, SupplyInformationDeliveryProtocol>(
                "SELECT * FROM SupplyInformationDeliveryProtocol " +
                "LEFT OUTER JOIN SupplyOrderInformationDeliveryProtocolKey " +
                "ON SupplyOrderInformationDeliveryProtocolKey.ID = SupplyInformationDeliveryProtocol.SupplyInformationDeliveryProtocolKeyID " +
                "AND SupplyOrderInformationDeliveryProtocolKey.Deleted = 0 " +
                "LEFT OUTER JOIN SupplyInvoice " +
                "ON SupplyInvoice.ID = SupplyInformationDeliveryProtocol.SupplyInvoiceID " +
                "AND SupplyInvoice.Deleted = 0 " +
                "LEFT OUTER JOIN SupplyOrder " +
                "ON SupplyOrder.ID = SupplyInformationDeliveryProtocol.SupplyOrderID " +
                "AND SupplyOrder.Deleted = 0 " +
                "LEFT OUTER JOIN [User] " +
                "ON [User].ID = SupplyInformationDeliveryProtocol.UserID " +
                "WHERE SupplyInformationDeliveryProtocol.NetUID = @NetId",
                (protocol, key, invoice, order, user) => {
                    if (key != null) protocol.SupplyInformationDeliveryProtocolKey = key;

                    if (invoice != null) protocol.SupplyInvoice = invoice;

                    if (order != null) protocol.SupplyOrder = order;

                    if (user != null) protocol.User = user;

                    return protocol;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE SupplyInformationDeliveryProtocol SET Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(SupplyInformationDeliveryProtocol protocol) {
        _connection.Execute(
            "UPDATE SupplyInformationDeliveryProtocol " +
            "SET SupplyProFormID = @SupplyProFormID, SupplyOrderID = @SupplyOrderID, UserID = @UserID, SupplyInvoiceID = @SupplyInvoiceID, " +
            "SupplyInformationDeliveryProtocolKeyID = @SupplyInformationDeliveryProtocolKeyID, [Value] = @Value, IsDefault = @IsDefault, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            protocol
        );
    }

    public void Update(IEnumerable<SupplyInformationDeliveryProtocol> protocols) {
        _connection.Execute(
            "UPDATE SupplyInformationDeliveryProtocol " +
            "SET SupplyProFormID = @SupplyProFormID, SupplyOrderID = @SupplyOrderID, UserID = @UserID, SupplyInvoiceID = @SupplyInvoiceID, Created = @Created, " +
            "SupplyInformationDeliveryProtocolKeyID = @SupplyInformationDeliveryProtocolKeyID, [Value] = @Value, IsDefault = @IsDefault, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            protocols
        );
    }

    public void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId) {
        _connection.Execute(
            "UPDATE [SupplyInformationDeliveryProtocol] " +
            "SET SupplyInvoiceID = @ToInvoiceId, Updated = GETUTCDATE() " +
            "WHERE SupplyInvoiceID = @FromInvoiceId",
            new { FromInvoiceId = fromInvoiceId, ToInvoiceId = toInvoiceId }
        );
    }
}