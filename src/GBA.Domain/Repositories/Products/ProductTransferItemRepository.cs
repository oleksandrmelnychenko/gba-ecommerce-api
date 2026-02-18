using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public class ProductTransferItemRepository : IProductTransferItemRepository {
    private readonly IDbConnection _connection;

    public ProductTransferItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductTransferItem item) {
        return _connection.Query<long>(
                "INSERT INTO [ProductTransferItem] (Qty, Reason, ProductId, ProductTransferId, ActReconciliationItemId, Updated) " +
                "VALUES (@Qty, @Reason, @ProductId, @ProductTransferId, @ActReconciliationItemId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                item
            )
            .SingleOrDefault();
    }

    public void Add(IEnumerable<ProductTransferItem> items) {
        _connection.Execute(
            "INSERT INTO [ProductTransferItem] (Qty, Reason, ProductId, ProductTransferId, ActReconciliationItemId, Updated) " +
            "VALUES (@Qty, @Reason, @ProductId, @ProductTransferId, @ActReconciliationItemId, GETUTCDATE())",
            items
        );
    }

    public void UpdateRemainingQty(ProductTransferItem item) {
        _connection.Execute(
            "UPDATE [ProductTransferItem] " +
            "SET RemainingQty = @RemainingQty " +
            "WHERE ID = @Id",
            item
        );
    }
}