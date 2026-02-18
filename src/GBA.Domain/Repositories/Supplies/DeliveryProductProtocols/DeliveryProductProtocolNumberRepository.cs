using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Repositories.Supplies.DeliveryProductProtocols.Contracts;

namespace GBA.Domain.Repositories.Supplies.DeliveryProductProtocols;

public sealed class DeliveryProductProtocolNumberRepository : IDeliveryProductProtocolNumberRepository {
    private readonly IDbConnection _connection;

    public DeliveryProductProtocolNumberRepository(IDbConnection connection) {
        _connection = connection;
    }

    public DeliveryProductProtocolNumber GetLastNumber(string defaultComment) {
        return _connection.Query<DeliveryProductProtocolNumber>(
            "SELECT TOP 1 * " +
            "FROM [DeliveryProductProtocolNumber] " +
            "WHERE [DeliveryProductProtocolNumber].[Number] != @Comment " +
            "ORDER BY [DeliveryProductProtocolNumber].[Created] DESC ",
            new { Comment = defaultComment }
        ).FirstOrDefault();
    }

    public long Add(DeliveryProductProtocolNumber number) {
        return _connection.Query<long>(
            "INSERT INTO [DeliveryProductProtocolNumber] ([Number], [Updated]) " +
            "VALUES (@Number, getutcdate()); " +
            "SELECT SCOPE_IDENTITY() ",
            number).Single();
    }
}