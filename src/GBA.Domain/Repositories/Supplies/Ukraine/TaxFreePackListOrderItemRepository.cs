using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class TaxFreePackListOrderItemRepository : ITaxFreePackListOrderItemRepository {
    private readonly IDbConnection _connection;

    public TaxFreePackListOrderItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(TaxFreePackListOrderItem taxFreePackListOrderItem) {
        return _connection.Query<long>(
            "INSERT INTO [TaxFreePackListOrderItem] " +
            "(NetWeight, Qty, UnpackedQty, OrderItemId, TaxFreePackListId, ConsignmentItemId, Updated) " +
            "VALUES " +
            "(@NetWeight, @Qty, @UnpackedQty, @OrderItemId, @TaxFreePackListId, @ConsignmentItemId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            taxFreePackListOrderItem
        ).Single();
    }

    public void Update(TaxFreePackListOrderItem taxFreePackListOrderItem) {
        _connection.Execute(
            "UPDATE [TaxFreePackListOrderItem] " +
            "SET NetWeight = @NetWeight, Qty = @Qty, UnpackedQty = @UnpackedQty, OrderItemId = @OrderItemId, " +
            "TaxFreePackListId = @TaxFreePackListId, ConsignmentItemId = @ConsignmentItemId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            taxFreePackListOrderItem
        );
    }

    public void Update(IEnumerable<TaxFreePackListOrderItem> items) {
        _connection.Execute(
            "UPDATE [TaxFreePackListOrderItem] " +
            "SET NetWeight = @NetWeight, Qty = @Qty, UnpackedQty = @UnpackedQty, OrderItemId = @OrderItemId, " +
            "TaxFreePackListId = @TaxFreePackListId, ConsignmentItemId = @ConsignmentItemId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            items
        );
    }

    public TaxFreePackListOrderItem GetById(long id) {
        return _connection.Query<TaxFreePackListOrderItem, OrderItem, Product, TaxFreePackListOrderItem>(
            "SELECT * " +
            "FROM [TaxFreePackListOrderItem] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "WHERE [TaxFreePackListOrderItem].ID = @Id",
            (item, orderItem, product) => {
                orderItem.Product = product;

                item.OrderItem = orderItem;

                return item;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public void RestoreUnpackedQtyByTaxFreeItemsIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [TaxFreePackListOrderItem] " +
            "SET [TaxFreePackListOrderItem].UnpackedQty = [TaxFreePackListOrderItem].UnpackedQty + [TaxFreeItem].Qty " +
            "FROM [TaxFreePackListOrderItem] " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].TaxFreePackListOrderItemID = [TaxFreePackListOrderItem].ID " +
            "AND [TaxFreeItem].Deleted = 0 " +
            "WHERE [TaxFreeItem].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RestoreUnpackedQtyByTaxFreeIdExceptProvidedIds(long taxFreeId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [TaxFreePackListOrderItem] " +
            "SET UnpackedQty = UnpackedQty + [TaxFreeItem].Qty, Updated = GETUTCDATE() " +
            "FROM [TaxFreePackListOrderItem] " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].TaxFreePackListOrderItemID = [TaxFreePackListOrderItem].ID " +
            "WHERE [TaxFreeItem].TaxFreeID = @TaxFreeId " +
            "AND [TaxFreeItem].ID NOT IN @Ids " +
            "AND [TaxFreeItem].Deleted = 0",
            new { TaxFreeId = taxFreeId, Ids = ids }
        );
    }

    public void DecreaseUnpackedQtyById(long id, double qty) {
        _connection.Execute(
            "UPDATE [TaxFreePackListOrderItem] " +
            "SET UnpackedQty = UnpackedQty - @Qty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id, Qty = qty }
        );
    }

    public void SetUnpackedQtyToAllItemsByOrderId(long orderId) {
        _connection.Execute(
            "UPDATE [TaxFreePackListOrderItem] " +
            "SET UnpackedQty = Qty " +
            "WHERE [TaxFreePackListOrderItem].OrderID = @OrderId",
            new { OrderId = orderId }
        );
    }
}