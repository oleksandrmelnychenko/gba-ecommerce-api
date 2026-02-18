using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyInvoiceOrderItemRepository : ISupplyInvoiceOrderItemRepository {
    private readonly IDbConnection _connection;

    public SupplyInvoiceOrderItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyInvoiceOrderItem supplyInvoiceOrderItem) {
        return _connection.Query<long>(
            "INSERT INTO [SupplyInvoiceOrderItem] (Qty, SupplyOrderItemId, SupplyInvoiceId, UnitPrice, Updated, [RowNumber], [ProductIsImported], [ProductId]) " +
            "VALUES (@Qty, @SupplyOrderItemId, @SupplyInvoiceId, @UnitPrice, getutcdate(), @RowNumber, @ProductIsImported, @ProductId); " +
            "SELECT SCOPE_IDENTITY()",
            supplyInvoiceOrderItem
        ).Single();
    }

    public void Add(IEnumerable<SupplyInvoiceOrderItem> supplyInvoiceOrderItems) {
        _connection.Execute(
            "INSERT INTO [SupplyInvoiceOrderItem] (Qty, SupplyOrderItemId, SupplyInvoiceId, UnitPrice, Updated, [RowNumber], [ProductIsImported], [ProductId]) " +
            "VALUES (@Qty, @SupplyOrderItemId, @SupplyInvoiceId, @UnitPrice, getutcdate(), @RowNumber, @ProductIsImported, @ProductId)",
            supplyInvoiceOrderItems
        );
    }

    public void Update(SupplyInvoiceOrderItem supplyInvoiceOrderItem) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceOrderItem] " +
            "SET Qty = @Qty, SupplyOrderItemId = @SupplyOrderItemId, SupplyInvoiceId = @SupplyInvoiceId, UnitPrice = @UnitPrice, Updated = getutcdate(), " +
            "[RowNumber] = @RowNumber, [ProductIsImported] = @ProductIsImported, [ProductId] = @ProductId " +
            "WHERE [SupplyInvoiceOrderItem].NetUID = @NetUid",
            supplyInvoiceOrderItem
        );
    }

    public void Update(IEnumerable<SupplyInvoiceOrderItem> supplyInvoiceOrderItems) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceOrderItem] " +
            "SET Qty = @Qty, SupplyOrderItemId = @SupplyOrderItemId, SupplyInvoiceId = @SupplyInvoiceId, UnitPrice = @UnitPrice, Updated = getutcdate(), " +
            "[RowNumber] = @RowNumber, [ProductIsImported] = @ProductIsImported, [ProductId] = @ProductId " +
            "WHERE [SupplyInvoiceOrderItem].NetUID = @NetUid",
            supplyInvoiceOrderItems
        );
    }

    public void UpdateSupplyInvoiceId(IEnumerable<SupplyInvoiceOrderItem> supplyInvoiceOrderItems) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceOrderItem] " +
            "SET SupplyInvoiceId = @SupplyInvoiceId, Updated = getutcdate() " +
            "WHERE [SupplyInvoiceOrderItem].NetUID = @NetUid",
            supplyInvoiceOrderItems
        );
    }

    public void RemoveAllByInvoiceId(long invoiceId) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceOrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [SupplyInvoiceOrderItem].SupplyInvoiceID = @InvoiceId",
            new { InvoiceId = invoiceId }
        );
    }

    public void RemoveAllByInvoiceIdExceptProvided(long invoiceId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceOrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [SupplyInvoiceOrderItem].SupplyInvoiceID = @InvoiceId AND [SupplyInvoiceOrderItem].ID NOT IN @Ids",
            new { InvoiceId = invoiceId, Ids = ids }
        );
    }

    public SupplyInvoiceOrderItem GetById(long id) {
        SupplyInvoiceOrderItem toReturn = null;

        _connection.Query<SupplyInvoiceOrderItem, PackingListPackageOrderItem, SupplyOrderItem, SupplyInvoiceOrderItem>(
            "SELECT [SupplyInvoiceOrderItem].*" +
            ", [PackingListPackageOrderItem].* " +
            ", [SupplyOrderItem].* " +
            "FROM [SupplyInvoiceOrderItem] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].SupplyInvoiceOrderItemID = [SupplyInvoiceOrderItem].ID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "WHERE [SupplyInvoiceOrderItem].ID = @Id " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "AND (" +
            "(" +
            "[PackingListPackageOrderItem].ID IS NOT NULL " +
            "AND " +
            "[PackingList].Deleted = 0" +
            ") " +
            "OR " +
            "[PackingListPackageOrderItem].ID IS NULL" +
            ")",
            (invoiceItem, packListItem, orderItem) => {
                if (toReturn != null) {
                    if (packListItem != null) toReturn.PackingListPackageOrderItems.Add(packListItem);
                } else {
                    if (packListItem != null) invoiceItem.PackingListPackageOrderItems.Add(packListItem);

                    invoiceItem.SupplyOrderItem = orderItem;

                    toReturn = invoiceItem;
                }

                return invoiceItem;
            },
            new { Id = id }
        );

        return toReturn;
    }

    public SupplyInvoiceOrderItem GetByInvoiceAndSupplyOrderItemIds(long invoiceId, long orderItemId) {
        SupplyInvoiceOrderItem toReturn = null;

        _connection.Query<SupplyInvoiceOrderItem, PackingListPackageOrderItem, SupplyOrderItem, SupplyInvoiceOrderItem>(
            "SELECT [SupplyInvoiceOrderItem].*" +
            ", [PackingListPackageOrderItem].* " +
            ", [SupplyOrderItem].* " +
            "FROM [SupplyInvoiceOrderItem] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].SupplyInvoiceOrderItemID = [SupplyInvoiceOrderItem].ID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "WHERE [SupplyInvoiceOrderItem].SupplyOrderItemID = @OrderItemId " +
            "AND [SupplyInvoiceOrderItem].SupplyInvoiceID = @InvoiceId " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "AND (" +
            "(" +
            "[PackingListPackageOrderItem].ID IS NOT NULL " +
            "AND " +
            "[PackingList].Deleted = 0" +
            ") " +
            "OR " +
            "[PackingListPackageOrderItem].ID IS NULL" +
            ")",
            (invoiceItem, packListItem, orderItem) => {
                if (toReturn != null) {
                    if (packListItem != null) toReturn.PackingListPackageOrderItems.Add(packListItem);
                } else {
                    if (packListItem != null) invoiceItem.PackingListPackageOrderItems.Add(packListItem);

                    invoiceItem.SupplyOrderItem = orderItem;

                    toReturn = invoiceItem;
                }

                return invoiceItem;
            },
            new { InvoiceId = invoiceId, OrderItemId = orderItemId }
        );

        return toReturn;
    }

    public SupplyInvoiceOrderItem GetByInvoiceAndProductIds(long invoiceId, long productId, double qty, decimal unitPrice) {
        SupplyInvoiceOrderItem toReturn = null;

        _connection.Query<SupplyInvoiceOrderItem, PackingListPackageOrderItem, SupplyOrderItem, SupplyInvoiceOrderItem>(
            "SELECT [SupplyInvoiceOrderItem].*" +
            ", [PackingListPackageOrderItem].* " +
            ", [SupplyOrderItem].* " +
            "FROM [SupplyInvoiceOrderItem] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].SupplyInvoiceOrderItemID = [SupplyInvoiceOrderItem].ID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "WHERE [SupplyInvoiceOrderItem].ProductID = @ProductId " +
            "AND [SupplyInvoiceOrderItem].SupplyInvoiceID = @InvoiceId " +
            "AND [SupplyInvoiceOrderItem].Qty = @Qty " +
            "AND [SupplyInvoiceOrderItem].UnitPrice = @UnitPrice " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "AND (" +
            "(" +
            "[PackingListPackageOrderItem].ID IS NOT NULL " +
            "AND " +
            "[PackingList].Deleted = 0" +
            ") " +
            "OR " +
            "[PackingListPackageOrderItem].ID IS NULL" +
            ")",
            (invoiceItem, packListItem, orderItem) => {
                if (toReturn != null) {
                    if (packListItem != null) toReturn.PackingListPackageOrderItems.Add(packListItem);
                } else {
                    if (packListItem != null) invoiceItem.PackingListPackageOrderItems.Add(packListItem);

                    invoiceItem.SupplyOrderItem = orderItem;

                    toReturn = invoiceItem;
                }

                return invoiceItem;
            },
            new { InvoiceId = invoiceId, ProductId = productId, Qty = qty, UnitPrice = unitPrice }
        );

        return toReturn;
    }
}