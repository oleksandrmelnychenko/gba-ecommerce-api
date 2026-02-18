using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class TaxFreeItemRepository : ITaxFreeItemRepository {
    private readonly IDbConnection _connection;

    public TaxFreeItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(TaxFreeItem item) {
        return _connection.Query<long>(
            "INSERT INTO [TaxFreeItem] (Qty, Comment, TaxFreeId, SupplyOrderUkraineCartItemId, TaxFreePackListOrderItemId, Updated) " +
            "VALUES (@Qty, @Comment, @TaxFreeId, @SupplyOrderUkraineCartItemId, @TaxFreePackListOrderItemId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            item
        ).Single();
    }

    public void Add(IEnumerable<TaxFreeItem> items) {
        _connection.Execute(
            "INSERT INTO [TaxFreeItem] (Qty, Comment, TaxFreeId, SupplyOrderUkraineCartItemId, TaxFreePackListOrderItemId, Updated) " +
            "VALUES (@Qty, @Comment, @TaxFreeId, @SupplyOrderUkraineCartItemId, @TaxFreePackListOrderItemId, GETUTCDATE())",
            items
        );
    }

    public void Update(TaxFreeItem item) {
        _connection.Execute(
            "UPDATE [TaxFreeItem] " +
            "SET Qty = @Qty, Comment = @Comment, TaxFreeId = @TaxFreeId, SupplyOrderUkraineCartItemId = @SupplyOrderUkraineCartItemId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            item
        );
    }

    public void Update(IEnumerable<TaxFreeItem> items) {
        _connection.Execute(
            "UPDATE [TaxFreeItem] " +
            "SET Qty = @Qty, Comment = @Comment, TaxFreeId = @TaxFreeId, SupplyOrderUkraineCartItemId = @SupplyOrderUkraineCartItemId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            items
        );
    }

    public void RemoveAllByTaxFreeIdExceptProvided(long taxFreeId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [TaxFreeItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE TaxFreeID = @TaxFreeId " +
            "AND ID NOT IN @Ids " +
            "AND Deleted = 0",
            new { TaxFreeId = taxFreeId, Ids = ids }
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [TaxFreeItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByPackListAndCartItemIds(long taxFreePackListId, IEnumerable<long> deletedItemIds) {
        _connection.Execute(
            "UPDATE [TaxFreeItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID IN (" +
            "SELECT [TaxFreeItem].ID " +
            "FROM [TaxFreeItem] " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFreeItem].TaxFreeID = [TaxFree].ID " +
            "WHERE [TaxFreeItem].Deleted = 0 " +
            "AND [TaxFree].TaxFreePackListID = @TaxFreePackListId " +
            "AND [TaxFreeItem].SupplyOrderUkraineCartItemID IN @DeletedItemIds" +
            ")",
            new { TaxFreePackListId = taxFreePackListId, DeletedItemIds = deletedItemIds }
        );
    }

    public void RemoveAllByOrderItemIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [TaxFreeItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE OrderItemID IN @Ids",
            new { Ids = ids }
        );
    }

    public TaxFreeItem GetById(long id) {
        return _connection.Query<TaxFreeItem, SupplyOrderUkraineCartItem, TaxFreeItem>(
            "SELECT * " +
            "FROM [TaxFreeItem] " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [TaxFreeItem].SupplyOrderUkraineCartItemID " +
            "WHERE [TaxFreeItem].ID = @Id",
            (item, cartItem) => {
                item.SupplyOrderUkraineCartItem = cartItem;

                return item;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public TaxFreeItem GetByTaxFreeAndCartItemIdsIfExists(long taxFreeId, long cartItemId) {
        return _connection.Query<TaxFreeItem>(
            "SELECT TOP(1) * " +
            "FROM [TaxFreeItem] " +
            "WHERE [TaxFreeItem].Deleted = 0 " +
            "AND [TaxFreeItem].TaxFreeID = @TaxFreeId " +
            "AND [TaxFreeItem].SupplyOrderUkraineCartItemID = @CartItemId",
            new { TaxFreeId = taxFreeId, CartItemId = cartItemId }
        ).FirstOrDefault();
    }

    public TaxFreeItem GetByTaxFreeAndPackListOrderItemIdsIfExists(long taxFreeId, long packListOrderItemId) {
        return _connection.Query<TaxFreeItem>(
            "SELECT TOP(1) * " +
            "FROM [TaxFreeItem] " +
            "WHERE [TaxFreeItem].Deleted = 0 " +
            "AND [TaxFreeItem].TaxFreeID = @TaxFreeId " +
            "AND [TaxFreeItem].TaxFreePackListOrderItemID = @PackListOrderItemId",
            new { TaxFreeId = taxFreeId, PackListOrderItemId = packListOrderItemId }
        ).FirstOrDefault();
    }

    public List<TaxFreeItem> GetAllByTaxFreeId(long taxFreeId) {
        List<TaxFreeItem> items =
            _connection.Query<TaxFreeItem>(
                "SELECT * " +
                "FROM [TaxFreeItem] " +
                "WHERE [TaxFreeItem].TaxFreeID = @TaxFreeId " +
                "AND [TaxFreeItem].Deleted = 0",
                new { TaxFreeId = taxFreeId }
            ).ToList();

        return items;
    }

    public List<TaxFreeItem> GetAllByTaxFreeIdExceptProvided(long taxFreeId, IEnumerable<long> ids) {
        List<TaxFreeItem> items =
            _connection.Query<TaxFreeItem>(
                "SELECT * " +
                "FROM [TaxFreeItem] " +
                "WHERE [TaxFreeItem].TaxFreeID = @TaxFreeId " +
                "AND [TaxFreeItem].ID NOT IN @Ids " +
                "AND [TaxFreeItem].Deleted = 0",
                new { TaxFreeId = taxFreeId, Ids = ids }
            ).ToList();

        return items;
    }
}