using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class DynamicProductPlacementColumnRepository : IDynamicProductPlacementColumnRepository {
    private readonly IDbConnection _connection;

    public DynamicProductPlacementColumnRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(DynamicProductPlacementColumn column) {
        return _connection.Query<long>(
                "INSERT INTO [DynamicProductPlacementColumn] (FromDate, SupplyOrderUkraineId, PackingListId, Updated) " +
                "VALUES (@FromDate, @SupplyOrderUkraineId, @PackingListId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                column
            )
            .Single();
    }

    public void Add(IEnumerable<DynamicProductPlacementColumn> columns) {
        _connection.Execute(
            "INSERT INTO [DynamicProductPlacementColumn] (FromDate, SupplyOrderUkraineId, PackingListId, Updated) " +
            "VALUES (@FromDate, @SupplyOrderUkraineId, @PackingListId, GETUTCDATE())",
            columns
        );
    }

    public void Update(DynamicProductPlacementColumn column) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacementColumn] " +
            "SET FromDate = @FromDate, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            column
        );
    }

    public void Update(IEnumerable<DynamicProductPlacementColumn> columns) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacementColumn] " +
            "SET FromDate = @FromDate, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            columns
        );
    }

    public void RemoveById(long id) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacementColumn] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }
}