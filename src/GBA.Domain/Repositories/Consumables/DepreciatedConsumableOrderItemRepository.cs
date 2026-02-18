using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class DepreciatedConsumableOrderItemRepository : IDepreciatedConsumableOrderItemRepository {
    private readonly IDbConnection _connection;

    public DepreciatedConsumableOrderItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(DepreciatedConsumableOrderItem depreciatedConsumableOrderItem) {
        return _connection.Query<long>(
                "INSERT INTO [DepreciatedConsumableOrderItem] " +
                "(Qty, DepreciatedConsumableOrderId, ConsumablesOrderItemId, Updated) " +
                "VALUES (@Qty, @DepreciatedConsumableOrderId, @ConsumablesOrderItemId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                depreciatedConsumableOrderItem
            )
            .Single();
    }

    public void Add(IEnumerable<DepreciatedConsumableOrderItem> depreciatedConsumableOrderItems) {
        _connection.Execute(
            "INSERT INTO [DepreciatedConsumableOrderItem] " +
            "(Qty, DepreciatedConsumableOrderId, ConsumablesOrderItemId, Updated) " +
            "VALUES (@Qty, @DepreciatedConsumableOrderId, @ConsumablesOrderItemId, getutcdate())",
            depreciatedConsumableOrderItems
        );
    }

    public void Update(IEnumerable<DepreciatedConsumableOrderItem> depreciatedConsumableOrderItems) {
        _connection.Execute(
            "UPDATE [DepreciatedConsumableOrderItem] " +
            "SET Qty = @Qty, DepreciatedConsumableOrderId = @DepreciatedConsumableOrderId, ConsumablesOrderItemId = @ConsumablesOrderItemId, Updated = getutcdate() " +
            "WHERE [DepreciatedConsumableOrderItem].ID = @Id",
            depreciatedConsumableOrderItems
        );
    }

    public void UpdateAndRestore(IEnumerable<DepreciatedConsumableOrderItem> depreciatedConsumableOrderItems) {
        _connection.Execute(
            "UPDATE [DepreciatedConsumableOrderItem] " +
            "SET Qty = @Qty, DepreciatedConsumableOrderId = @DepreciatedConsumableOrderId, ConsumablesOrderItemId = @ConsumablesOrderItemId, Deleted = 0, Updated = getutcdate() " +
            "WHERE [DepreciatedConsumableOrderItem].ID = @Id",
            depreciatedConsumableOrderItems
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [DepreciatedConsumableOrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [DepreciatedConsumableOrderItem].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByOrderId(long id) {
        _connection.Execute(
            "UPDATE [DepreciatedConsumableOrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [DepreciatedConsumableOrderItem].DepreciatedConsumableOrderID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByOrderAndProductIds(long orderId, long productId) {
        _connection.Execute(
            "UPDATE [DepreciatedConsumableOrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "FROM [DepreciatedConsumableOrderItem] " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ID = [DepreciatedConsumableOrderItem].ConsumablesOrderItemID " +
            "WHERE [DepreciatedConsumableOrderItem].DepreciatedConsumableOrderID = @OrderId " +
            "AND [ConsumablesOrderItem].ConsumableProductID = @ProductId",
            new { OrderId = orderId, ProductId = productId }
        );
    }

    public IEnumerable<DepreciatedConsumableOrderItem> GetAllByOrderId(long id) {
        return _connection.Query<DepreciatedConsumableOrderItem>(
            "SELECT * " +
            "FROM [DepreciatedConsumableOrderItem] " +
            "WHERE [DepreciatedConsumableOrderItem].DepreciatedConsumableOrderID = @Id " +
            "AND [DepreciatedConsumableOrderItem].Deleted = 0",
            new { Id = id }
        );
    }
}