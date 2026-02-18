using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Repositories.DepreciatedOrders.Contracts;

namespace GBA.Domain.Repositories.DepreciatedOrders;

public sealed class DepreciatedOrderItemRepository : IDepreciatedOrderItemRepository {
    private readonly IDbConnection _connection;

    public DepreciatedOrderItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(DepreciatedOrderItem item) {
        return _connection.Query<long>(
                "INSERT INTO [DepreciatedOrderItem] (Qty, Reason, ProductId, DepreciatedOrderId, ActReconciliationItemId, Updated) " +
                "VALUES (@Qty, @Reason, @ProductId, @DepreciatedOrderId, @ActReconciliationItemId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                item
            )
            .Single();
    }

    public void Add(IEnumerable<DepreciatedOrderItem> items) {
        _connection.Execute(
            "INSERT INTO [DepreciatedOrderItem] (Qty, Reason, ProductId, DepreciatedOrderId, Updated) " +
            "VALUES (@Qty, @Reason, @ProductId, @DepreciatedOrderId, GETUTCDATE())",
            items
        );
    }
}