using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductLocationHistoryRepository : IProductLocationHistoryRepository {
    private readonly IDbConnection _connection;

    public ProductLocationHistoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ProductLocationHistory productLocationHistory) {
        _connection.Execute(
            "INSERT INTO [ProductLocationHistory] (Qty, StorageId, ProductPlacementId, OrderItemId, DepreciatedOrderItemId, HistoryInvoiceEditId, TypeOfMovement, Updated) " +
            "VALUES (@Qty, @StorageId, @ProductPlacementId, @OrderItemId, @DepreciatedOrderItemId, @HistoryInvoiceEditId, @TypeOfMovement, GETUTCDATE())",
            productLocationHistory
        );
    }

    public void Update(ProductLocationHistory productLocationHistory) {
        _connection.Execute(
            "UPDATE [ProductLocationHistory] " +
            "SET Qty = @Qty, ProductPlacementId = @ProductPlacementId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            productLocationHistory
        );
    }

    public void UpdateIvoiceDocumentQty(ProductLocationHistory productLocationHistory) {
        _connection.Execute(
            "UPDATE [ProductLocationHistory] " +
            "SET InvoiceDocumentQty = @InvoiceDocumentQty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            productLocationHistory
        );
    }

    public void Remove(ProductLocationHistory productLocationHistory) {
        _connection.Execute(
            "UPDATE [ProductLocationHistory] " +
            "SET Qty = @Qty, ProductPlacementId = @ProductPlacementId, Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            productLocationHistory
        );
    }

    public IEnumerable<ProductLocationHistory> GetAllByOrderItemId(long id) {
        return _connection.Query<ProductLocationHistory, ProductPlacement, ProductLocationHistory>(
            "SELECT * " +
            "FROM [ProductLocationHistory] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductLocationHistory].ProductPlacementID " +
            "WHERE [ProductLocationHistory].Deleted = 0 " +
            "AND [ProductLocationHistory].OrderItemID = @Id",
            (location, placement) => {
                location.ProductPlacement = placement;

                return location;
            },
            new { Id = id }
        );
    }

    public IEnumerable<ProductLocationHistory> GetAllByOrderItemIdDeleted(long id) {
        return _connection.Query<ProductLocationHistory, ProductPlacement, ProductLocationHistory>(
            "SELECT * " +
            "FROM [ProductLocationHistory] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductLocationHistory].ProductPlacementID " +
            "WHERE [ProductLocationHistory].OrderItemID = @Id",
            (location, placement) => {
                location.ProductPlacement = placement;

                return location;
            },
            new { Id = id }
        );
    }
}