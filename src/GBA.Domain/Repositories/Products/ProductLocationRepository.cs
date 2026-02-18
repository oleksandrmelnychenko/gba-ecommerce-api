using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductLocationRepository : IProductLocationRepository {
    private readonly IDbConnection _connection;

    public ProductLocationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ProductLocation productLocation) {
        _connection.Execute(
            "INSERT INTO [ProductLocation] (Qty, StorageId, ProductPlacementId, OrderItemId, DepreciatedOrderItemId, ProductTransferItemId, Updated) " +
            "VALUES (@Qty, @StorageId, @ProductPlacementId, @OrderItemId, @DepreciatedOrderItemId, @ProductTransferItemId, GETUTCDATE())",
            productLocation
        );
    }

    public void Update(ProductLocation productLocation) {
        _connection.Execute(
            "UPDATE [ProductLocation] " +
            "SET Qty = @Qty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            productLocation
        );
    }

    public void UpdateIvoiceDocumentQty(ProductLocation productLocation) {
        _connection.Execute(
            "UPDATE [ProductLocation] " +
            "SET InvoiceDocumentQty = @InvoiceDocumentQty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            productLocation
        );
    }

    public void Remove(ProductLocation productLocation) {
        _connection.Execute(
            "UPDATE [ProductLocation] " +
            "SET Qty = @Qty, Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            productLocation
        );
    }

    public IEnumerable<ProductLocation> GetAllByOrderItemId(long id) {
        return _connection.Query<ProductLocation, ProductPlacement, ProductLocation>(
            "SELECT * " +
            "FROM [ProductLocation] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
            "WHERE [ProductLocation].Deleted = 0 " +
            "AND [ProductLocation].OrderItemID = @Id",
            (location, placement) => {
                location.ProductPlacement = placement;

                return location;
            },
            new { Id = id }
        );
    }

    public IEnumerable<ProductLocation> GetAllByOrderItemIdDeleted(long id) {
        return _connection.Query<ProductLocation, ProductPlacement, ProductLocation>(
            "SELECT * " +
            "FROM [ProductLocation] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
            "WHERE [ProductLocation].OrderItemID = @Id",
            (location, placement) => {
                location.ProductPlacement = placement;

                return location;
            },
            new { Id = id }
        );
    }
}