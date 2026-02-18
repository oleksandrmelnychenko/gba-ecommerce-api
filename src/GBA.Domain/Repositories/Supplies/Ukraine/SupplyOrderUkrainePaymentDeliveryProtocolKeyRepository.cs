using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SupplyOrderUkrainePaymentDeliveryProtocolKeyRepository : ISupplyOrderUkrainePaymentDeliveryProtocolKeyRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderUkrainePaymentDeliveryProtocolKeyRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyOrderUkrainePaymentDeliveryProtocolKey protocolKey) {
        return _connection.Query<long>(
                "INSERT INTO [SupplyOrderUkrainePaymentDeliveryProtocolKey] ([Key], Updated) " +
                "VALUES (@Key, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                protocolKey
            )
            .Single();
    }

    public void Add(IEnumerable<SupplyOrderUkrainePaymentDeliveryProtocolKey> protocolKeys) {
        _connection.Execute(
            "INSERT INTO [SupplyOrderUkrainePaymentDeliveryProtocolKey] ([Key], Updated) " +
            "VALUES (@Key, GETUTCDATE())",
            protocolKeys
        );
    }

    public void Update(SupplyOrderUkrainePaymentDeliveryProtocolKey protocolKey) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkrainePaymentDeliveryProtocolKey] " +
            "SET [Key] = @Key, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            protocolKey
        );
    }

    public void Update(IEnumerable<SupplyOrderUkrainePaymentDeliveryProtocolKey> protocolKeys) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkrainePaymentDeliveryProtocolKey] " +
            "SET [Key] = @Key, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            protocolKeys
        );
    }

    public IEnumerable<SupplyOrderUkrainePaymentDeliveryProtocolKey> GetAll() {
        return _connection.Query<SupplyOrderUkrainePaymentDeliveryProtocolKey>(
            "SELECT * " +
            "FROM [SupplyOrderUkrainePaymentDeliveryProtocolKey] " +
            "WHERE ID IN " +
            "(" +
            "SELECT MAX(ID) " +
            "FROM [SupplyOrderUkrainePaymentDeliveryProtocolKey] " +
            "WHERE Deleted = 0 " +
            "GROUP BY [Key]" +
            ")"
        );
    }
}