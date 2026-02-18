using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class OrderItemMergedRepository : IOrderItemMergedRepository {
    private readonly IDbConnection _connection;

    public OrderItemMergedRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(OrderItemMerged orderItemMerged) {
        return _connection.Query<long>(
                "INSERT INTO OrderItemMerged (OldOrderID, OrderItemID, OldOrderItemID, Updated) " +
                "VALUES(@OldOrderId, @OrderItemId, @OldOrderItemId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                orderItemMerged
            )
            .Single();
    }

    public void Add(List<OrderItemMerged> orderItemsMerged) {
        _connection.Execute(
            "INSERT INTO OrderItemMerged (OldOrderID, OrderItemID, OldOrderItemID, Updated) " +
            "VALUES(@OldOrderId, @OrderItemId, @OldOrderItemId, getutcdate()); ",
            orderItemsMerged
        );
    }
}