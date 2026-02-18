using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Protocols;

public sealed class SupplyOrderPaymentDeliveryProtocolKeyRepository : ISupplyOrderPaymentDeliveryProtocolKeyRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderPaymentDeliveryProtocolKeyRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyOrderPaymentDeliveryProtocolKey key) {
        return _connection.Query<long>(
                "INSERT INTO SupplyOrderPaymentDeliveryProtocolKey([Key], Updated) VALUES(@Key, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                key
            )
            .Single();
    }

    public void Add(IEnumerable<SupplyOrderPaymentDeliveryProtocolKey> keys) {
        _connection.Execute(
            "INSERT INTO SupplyOrderPaymentDeliveryProtocolKey([Key], Updated) VALUES(@Key, getutcdate())",
            keys
        );
    }

    public List<SupplyOrderPaymentDeliveryProtocolKey> GetAll() {
        return _connection.Query<SupplyOrderPaymentDeliveryProtocolKey>(
                "SELECT * FROM SupplyOrderPaymentDeliveryProtocolKey " +
                "WHERE ID IN " +
                "(SELECT MAX(ID) " +
                "FROM [SupplyOrderPaymentDeliveryProtocolKey] " +
                "WHERE Deleted = 0 " +
                "GROUP BY [Key])"
            )
            .ToList();
    }

    public void Update(SupplyOrderPaymentDeliveryProtocolKey key) {
        _connection.Execute(
            "UPDATE SupplyOrderPaymentDeliveryProtocolKey SET [Key] = @Key, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            key
        );
    }

    public void Update(IEnumerable<SupplyOrderPaymentDeliveryProtocolKey> keys) {
        _connection.Execute(
            "UPDATE SupplyOrderPaymentDeliveryProtocolKey SET [Key] = @Key, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            keys
        );
    }
}