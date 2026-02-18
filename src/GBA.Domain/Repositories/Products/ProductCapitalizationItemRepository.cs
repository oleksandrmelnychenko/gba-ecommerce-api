using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductCapitalizationItemRepository : IProductCapitalizationItemRepository {
    private readonly IDbConnection _connection;

    public ProductCapitalizationItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<ProductCapitalizationItem> items) {
        _connection.Execute(
            "INSERT INTO [ProductCapitalizationItem] (Qty, RemainingQty, Weight, UnitPrice, ProductId, ProductCapitalizationId, Updated) " +
            "VALUES (@Qty, @RemainingQty, @Weight, @UnitPrice, @ProductId, @ProductCapitalizationId, GETUTCDATE())",
            items
        );
    }

    public long Add(ProductCapitalizationItem item) {
        return _connection.Query<long>(
            "INSERT INTO [ProductCapitalizationItem] (Qty, RemainingQty, Weight, UnitPrice, ProductId, ProductCapitalizationId, Updated) " +
            "VALUES (@Qty, @RemainingQty, @Weight, @UnitPrice, @ProductId, @ProductCapitalizationId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            item
        ).Single();
    }

    public void UpdateRemainingQty(ProductCapitalizationItem item) {
        _connection.Query<long>(
            "UPDATE [ProductCapitalizationItem] " +
            "SET RemainingQty = @RemainingQty " +
            "WHERE ID = @Id",
            item
        );
    }
}