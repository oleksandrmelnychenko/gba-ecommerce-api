using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class ActReconciliationItemRepository : IActReconciliationItemRepository {
    private readonly IDbConnection _connection;

    public ActReconciliationItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ActReconciliationItem actReconciliationItem) {
        return _connection.Query<long>(
                "INSERT INTO [ActReconciliationItem] " +
                "(HasDifference, NegativeDifference, OrderedQty, ActualQty, QtyDifference, CommentUA, CommentPL, ProductId, ActReconciliationId, " +
                "NetWeight, UnitPrice, SupplyOrderUkraineItemId, SupplyInvoiceOrderItemId, Updated) " +
                "VALUES (@HasDifference, @NegativeDifference, @OrderedQty, @ActualQty, @QtyDifference, @CommentUA, @CommentPL, @ProductId, @ActReconciliationId, " +
                "@NetWeight, @UnitPrice, @SupplyOrderUkraineItemId, @SupplyInvoiceOrderItemId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                actReconciliationItem
            )
            .Single();
    }

    public void Add(IEnumerable<ActReconciliationItem> actReconciliationItems) {
        _connection.Execute(
            "INSERT INTO [ActReconciliationItem] " +
            "(HasDifference, NegativeDifference, OrderedQty, ActualQty, QtyDifference, CommentUA, CommentPL, ProductId, ActReconciliationId, " +
            "NetWeight, UnitPrice, SupplyOrderUkraineItemId, SupplyInvoiceOrderItemId, Updated) " +
            "VALUES (@HasDifference, @NegativeDifference, @OrderedQty, @ActualQty, @QtyDifference, @CommentUA, @CommentPL, @ProductId, @ActReconciliationId, " +
            "@NetWeight, @UnitPrice, @SupplyOrderUkraineItemId, @SupplyInvoiceOrderItemId, GETUTCDATE())",
            actReconciliationItems
        );
    }

    public void Update(ActReconciliationItem actReconciliationItem) {
        _connection.Execute(
            "UPDATE [ActReconciliationItem] " +
            "SET HasDifference = @HasDifference, NegativeDifference = @NegativeDifference, QtyDifference = @QtyDifference, [ActualQty] = @ActualQty " +
            "WHERE [ActReconciliationItem].ID = @Id",
            actReconciliationItem
        );
    }

    public void FullUpdate(ActReconciliationItem actReconciliationItem) {
        _connection.Execute(
            "UPDATE [ActReconciliationItem] " +
            "SET HasDifference = @HasDifference, NegativeDifference = @NegativeDifference, OrderedQty = @OrderedQty, ActualQty = @ActualQty, " +
            "QtyDifference = @QtyDifference, CommentUA = @CommentUA, CommentPL = @CommentPL, NetWeight = @NetWeight, UnitPrice = @UnitPrice, Updated = GETUTCDATE() " +
            "WHERE [ActReconciliationItem].ID = @Id",
            actReconciliationItem
        );
    }

    public void RemoveAllBySupplyOrderUkraineItemIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ActReconciliationItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "FROM [ActReconciliationItem] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ActReconciliationItem].SupplyOrderUkraineItemID " +
            "WHERE [SupplyOrderUkraineItem].ID IN @Ids " +
            "AND [ActReconciliationItem].Deleted = 0",
            new { Ids = ids }
        );
    }

    public void RemoveAllByActReconciliationIdExceptProvidedSupplyOrderUkraineItemIds(long actReconciliationId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ActReconciliationItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "FROM [ActReconciliationItem] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ActReconciliationItem].SupplyOrderUkraineItemID " +
            "WHERE [SupplyOrderUkraineItem].ID NOT IN @Ids " +
            "AND [ActReconciliationItem].ActReconciliationID = @ActReconciliationId " +
            "AND [ActReconciliationItem].Deleted = 0",
            new { Ids = ids, ActReconciliationId = actReconciliationId }
        );
    }

    public ActReconciliationItem GetById(long id) {
        return _connection.Query<ActReconciliationItem, SupplyOrderUkraineItem, SupplyInvoiceOrderItem, ActReconciliationItem>(
                "SELECT * " +
                "FROM [ActReconciliationItem] " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].ID = [ActReconciliationItem].SupplyOrderUkraineItemID " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [SupplyInvoiceOrderItem].ID = [ActReconciliationItem].SupplyInvoiceOrderItemID " +
                "WHERE [ActReconciliationItem].ID = @Id",
                (item, ukraineItem, invoiceItem) => {
                    item.SupplyOrderUkraineItem = ukraineItem;
                    item.SupplyInvoiceOrderItem = invoiceItem;

                    return item;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public ActReconciliationItem GetBySupplyOrderUkraineItemId(long id) {
        return _connection.Query<ActReconciliationItem, SupplyOrderUkraineItem, ActReconciliationItem>(
                "SELECT * " +
                "FROM [ActReconciliationItem] " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].ID = [ActReconciliationItem].SupplyOrderUkraineItemID " +
                "WHERE [SupplyOrderUkraineItem].ID = @Id",
                (item, ukraineItem) => {
                    item.SupplyOrderUkraineItem = ukraineItem;

                    return item;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public ActReconciliationItem GetBySupplyInvoiceOrderItemId(long id) {
        return _connection.Query<ActReconciliationItem, SupplyInvoiceOrderItem, ActReconciliationItem>(
                "SELECT * " +
                "FROM [ActReconciliationItem] " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [SupplyInvoiceOrderItem].ID = [ActReconciliationItem].SupplyInvoiceOrderItemID " +
                "WHERE [SupplyInvoiceOrderItem].ID = @Id",
                (item, invoiceItem) => {
                    item.SupplyInvoiceOrderItem = invoiceItem;

                    return item;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public ActReconciliationItem GetByNetId(Guid netId) {
        return _connection.Query<ActReconciliationItem, SupplyOrderUkraineItem, SupplyInvoiceOrderItem, ActReconciliationItem>(
                "SELECT * " +
                "FROM [ActReconciliationItem] " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].ID = [ActReconciliationItem].SupplyOrderUkraineItemID " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [SupplyInvoiceOrderItem].ID = [ActReconciliationItem].SupplyInvoiceOrderItemID " +
                "WHERE [ActReconciliationItem].NetUID = @NetId",
                (item, ukraineItem, invoiceItem) => {
                    item.SupplyOrderUkraineItem = ukraineItem;
                    item.SupplyInvoiceOrderItem = invoiceItem;

                    return item;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public IEnumerable<ActReconciliationItem> GetByIds(IEnumerable<long> ids) {
        return _connection.Query<ActReconciliationItem>(
            "SELECT * " +
            "FROM [ActReconciliationItem] " +
            "WHERE [ActReconciliationItem].ID IN @Ids",
            new { Ids = ids }
        );
    }
}