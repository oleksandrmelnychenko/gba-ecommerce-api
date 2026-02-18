using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SadItemRepository : ISadItemRepository {
    private readonly IDbConnection _connection;

    public SadItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SadItem item) {
        return _connection.Query<long>(
            "INSERT INTO [SadItem] " +
            "(Qty, Comment, SadId, SupplyOrderUkraineCartItemId, NetWeight, UnitPrice, OrderItemId, SupplierId, ConsignmentItemId, UnpackedQty, Updated) " +
            "VALUES " +
            "(@Qty, @Comment, @SadId, @SupplyOrderUkraineCartItemId, @NetWeight, @UnitPrice, @OrderItemId, @SupplierId, @ConsignmentItemId, @UnpackedQty, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            item
        ).Single();
    }

    public void Add(IEnumerable<SadItem> items) {
        _connection.Execute(
            "INSERT INTO [SadItem] (Qty, Comment, SadId, SupplyOrderUkraineCartItemId, Updated) " +
            "VALUES (@Qty, @Comment, @SadId, @SupplyOrderUkraineCartItemId, GETUTCDATE())",
            items
        );
    }

    public void Update(SadItem item) {
        _connection.Execute(
            "UPDATE [SadItem] " +
            "SET Qty = @Qty, Comment = @Comment, Deleted = @Deleted, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            item
        );
    }

    public void Update(IEnumerable<SadItem> items) {
        _connection.Execute(
            "UPDATE [SadItem] " +
            "SET Qty = @Qty, Comment = @Comment, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            items
        );
    }

    public void RemoveAllBySadIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SadItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE SadID = @Id " +
            "AND ID NOT IN @Ids " +
            "AND Deleted = 0",
            new { Id = id, Ids = ids }
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SadItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RestoreUnpackedQtyById(long id, double qty) {
        _connection.Execute(
            "UPDATE [SadItem] " +
            "SET UnpackedQty = UnpackedQty + @Qty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id, Qty = qty }
        );
    }

    public void DecreaseUnpackedQtyById(long id, double qty) {
        _connection.Execute(
            "UPDATE [SadItem] " +
            "SET UnpackedQty = UnpackedQty - @Qty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id, Qty = qty }
        );
    }

    public IEnumerable<SadItem> GetAllBySadIdExceptProvided(long sadId, IEnumerable<long> ids) {
        return _connection.Query<SadItem, SupplyOrderUkraineCartItem, SadItem>(
            "SELECT * " +
            "FROM [SadItem] " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SadItem].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "WHERE [SadItem].SadID = @SadId " +
            "AND [SadItem].ID NOT IN @Ids " +
            "AND [SadItem].Deleted = 0",
            (item, cartItem) => {
                item.SupplyOrderUkraineCartItem = cartItem;

                return item;
            },
            new { SadId = sadId, Ids = ids }
        );
    }

    public IEnumerable<SadItem> GetAllItemsForRemoveBySadIdExceptProvidedItemIds(long sadId, IEnumerable<long> ids) {
        List<SadItem> items = new();

        _connection.Query<SadItem, SupplyOrderUkraineCartItem, SupplyOrderUkraineCartItemReservation, ProductAvailability, ConsignmentItem, SadItem>(
            "SELECT * " +
            "FROM [SadItem] " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SadItem].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "LEFT JOIN [SupplyOrderUkraineCartItemReservation] " +
            "ON [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "AND [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [SupplyOrderUkraineCartItemReservation].ProductAvailabilityID " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [SupplyOrderUkraineCartItemReservation].ConsignmentItemID " +
            "WHERE [SadItem].SadID = @SadId " +
            "AND [SadItem].ID NOT IN @Ids " +
            "AND [SadItem].Deleted = 0",
            (item, cartItem, cartItemReservation, availability, consignmentItem) => {
                if (items.Any(i => i.Id.Equals(item.Id))) {
                    item = items.First(i => i.Id.Equals(item.Id));
                } else {
                    item.SupplyOrderUkraineCartItem = cartItem;

                    items.Add(item);
                }

                if (cartItemReservation == null) return item;

                cartItemReservation.ProductAvailability = availability;
                cartItemReservation.ConsignmentItem = consignmentItem;

                item.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations.Add(cartItemReservation);

                return item;
            },
            new { SadId = sadId, Ids = ids }
        );

        return items;
    }

    public SadItem GetById(long id) {
        return _connection.Query<SadItem, SupplyOrderUkraineCartItem, OrderItem, Product, Product, SadItem>(
            "SELECT * " +
            "FROM [SadItem] " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SadItem].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "LEFT JOIN [OrderItem] " +
            "ON [SadItem].OrderItemID = [OrderItem].ID " +
            "LEFT JOIN [Product] AS [CartProduct] " +
            "ON [CartProduct].Id = [SupplyOrderUkraineCartItem].ProductID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "WHERE [SadItem].ID = @Id",
            (item, cartItem, orderItem, cartProduct, product) => {
                if (cartItem != null) cartItem.Product = cartProduct;

                if (orderItem != null) orderItem.Product = product;

                item.SupplyOrderUkraineCartItem = cartItem;
                item.OrderItem = orderItem;

                return item;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public SadItem GetByIdWithoutIncludes(long id) {
        return _connection.Query<SadItem>(
            "SELECT * " +
            "FROM [SadItem] " +
            "WHERE [SadItem].ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }
}