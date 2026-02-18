using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyReturnItemRepository : ISupplyReturnItemRepository {
    private readonly IDbConnection _connection;

    public SupplyReturnItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyReturnItem item) {
        return _connection.Query<long>(
            "INSERT INTO [SupplyReturnItem] (Qty, ProductId, SupplyReturnId, ConsignmentItemId, Updated) " +
            "VALUES (@Qty, @ProductId, @SupplyReturnId, @ConsignmentItemId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            item
        ).Single();
    }

    public void Add(IEnumerable<SupplyReturnItem> items) {
        _connection.Execute(
            "INSERT INTO [SupplyReturnItem] (Qty, ProductId, SupplyReturnId, ConsignmentItemId, Updated) " +
            "VALUES (@Qty, @ProductId, @SupplyReturnId, @ConsignmentItemId, GETUTCDATE())",
            items
        );
    }

    public void RemoveAllBySupplyReturnId(long id) {
        _connection.Execute(
            "UPDATE [SupplyReturnItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE SupplyReturnID = @Id ",
            new { Id = id }
        );
    }
}