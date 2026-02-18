using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductIncomeItemRepository : IProductIncomeItemRepository {
    private readonly IDbConnection _connection;

    public ProductIncomeItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<ProductIncomeItem> items) {
        _connection.Execute(
            "INSERT INTO [ProductIncomeItem] " +
            "(SaleReturnItemId, ProductIncomeId, PackingListPackageOrderItemId, SupplyOrderUkraineItemId, Qty, RemainingQty, ProductCapitalizationItemId, Updated) " +
            "VALUES (@SaleReturnItemId, @ProductIncomeId, @PackingListPackageOrderItemId, @SupplyOrderUkraineItemId, @Qty, @RemainingQty, @ProductCapitalizationItemId, GETUTCDATE())",
            items
        );
    }

    public long Add(ProductIncomeItem item) {
        return _connection.Query<long>(
                "INSERT INTO [ProductIncomeItem] (SaleReturnItemId, ProductIncomeId, PackingListPackageOrderItemId, SupplyOrderUkraineItemId, Qty, RemainingQty, " +
                "ActReconciliationItemId, ProductCapitalizationItemId, Updated) " +
                "VALUES (@SaleReturnItemId, @ProductIncomeId, @PackingListPackageOrderItemId, @SupplyOrderUkraineItemId, @Qty, @RemainingQty, @ActReconciliationItemId, " +
                "@ProductCapitalizationItemId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                item
            )
            .Single();
    }

    public void UpdateRemainingQty(ProductIncomeItem item) {
        _connection.Execute(
            "UPDATE [ProductIncomeItem] " +
            "SET RemainingQty = @RemainingQty " +
            "WHERE ID = @Id",
            item
        );
    }

    public void UpdateQtyFields(ProductIncomeItem item) {
        _connection.Execute(
            "UPDATE [ProductIncomeItem] " +
            "SET Qty = @Qty, RemainingQty = @RemainingQty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            item
        );
    }

    public ProductIncomeItem GetByProductIncomeAndPackingListPackageOrderItemIdsIfExists(long productIncomeId, long packingListPackageOrderItemId) {
        return _connection.Query<ProductIncomeItem>(
            "SELECT TOP(1) * " +
            "FROM [ProductIncomeItem] " +
            "WHERE [ProductIncomeItem].Deleted = 0 " +
            "AND [ProductIncomeItem].PackingListPackageOrderItemID = @PackingListPackageOrderItemId " +
            "AND [ProductIncomeItem].ProductIncomeID = @ProductIncomeId",
            new { ProductIncomeId = productIncomeId, PackingListPackageOrderItemId = packingListPackageOrderItemId }
        ).SingleOrDefault();
    }

    public ProductIncomeItem
        GetByProductIncomeAndSupplyOrderUkraineItemIdsIfExists(long productIncomeId, long supplyOrderUkraineItemId, long? packingListPackageOrderItemId) {
        return _connection.Query<ProductIncomeItem>(
            "SELECT TOP(1) * " +
            "FROM [ProductIncomeItem] " +
            "WHERE [ProductIncomeItem].Deleted = 0 " +
            "AND [ProductIncomeItem].SupplyOrderUkraineItemID = @SupplyOrderUkraineItemId " +
            (
                packingListPackageOrderItemId.HasValue
                    ? "AND [ProductIncomeItem].PackingListPackageOrderItemID = @PackingListPackageOrderItemId "
                    : "AND [ProductIncomeItem].PackingListPackageOrderItemID IS NULL "
            ) +
            "AND [ProductIncomeItem].ProductIncomeID = @ProductIncomeId",
            new {
                ProductIncomeId = productIncomeId, SupplyOrderUkraineItemId = supplyOrderUkraineItemId, PackingListPackageOrderItemId = packingListPackageOrderItemId
            }
        ).SingleOrDefault();
    }
}