using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Repositories.ReSales.Contracts;

namespace GBA.Domain.Repositories.ReSales;

public sealed class ReSaleItemRepository : IReSaleItemRepository {
    private readonly IDbConnection _connection;

    public ReSaleItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ReSaleItem item) {
        return _connection.Query<long>(
            "INSERT INTO [ReSaleItem] " +
            "([Updated], [ReSaleID], [ReSaleAvailabilityID], [Qty], [PricePerItem], [ExchangeRate], [ExtraCharge], [ProductID]) " +
            "VALUES " +
            "(GETUTCDATE(), @ReSaleId, @ReSaleAvailabilityId, @Qty, @PricePerItem, @ExchangeRate, @ExtraCharge, @ProductId); " +
            "SELECT SCOPE_IDENTITY(); ",
            item
        ).FirstOrDefault();
    }

    public void Update(ReSaleItem item) {
        _connection.Execute(
            "UPDATE [ReSaleItem] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [Qty] = @Qty " +
            ", [PricePerItem] = @PricePerItem " +
            ", [ExchangeRate] = @ExchangeRate " +
            ", [ExtraCharge] = @ExtraCharge " +
            "WHERE ID = @Id",
            item
        );
    }

    public void UpdateMany(IEnumerable<ReSaleItem> items) {
        _connection.Execute(
            "UPDATE [ReSaleItem] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [Qty] = @Qty " +
            ", [PricePerItem] = @PricePerItem " +
            ", [ExchangeRate] = @ExchangeRate " +
            ", [ExtraCharge] = @ExtraCharge " +
            "WHERE ID = @Id",
            items
        );
    }

    public void Delete(long id) {
        _connection.Execute(
            "UPDATE [ReSaleItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public ReSaleItem GetById(long id) {
        return _connection.Query<ReSaleItem>(
            "SELECT * FROM [ReSaleItem] " +
            "WHERE [ReSaleItem].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }

    public ReSaleItem GetEmptyReSaleItemsByProductId(
        long reSaleId,
        long productId) {
        return _connection.Query<ReSaleItem>(
            "SELECT TOP 1 * FROM [ReSaleItem] " +
            "WHERE [ReSaleItem].[ReSaleID] = @ReSaleId " +
            "AND [ReSaleItem].[ProductID] = @ProductId " +
            "AND [ReSaleItem].[ReSaleAvailabilityID] IS NULL ",
            new { ReSaleId = reSaleId, ProductId = productId }).FirstOrDefault();
    }

    public int GetSoldInReSaleByProductIdAndReSaleId(long productId, long reSaleId) {
        return _connection.Query<int>(
            "SELECT ISNULL(SUM(ReSaleItem.Qty), 0) FROM ReSaleItem " +
            "LEFT JOIN ReSale " +
            "ON ReSaleItem.ReSaleID = ReSale.ID " +
            "WHERE ReSaleItem.ProductID = @ProductID " +
            "AND ReSale.ChangedToInvoice IS NULL " +
            "AND ReSale.ID <> @ReSaleId " +
            "AND ReSaleItem.Deleted = 0 ",
            new { ProductId = productId, ReSaleId = reSaleId }).FirstOrDefault();
    }

    public void DeleteByReSale(long reSaleId) {
        _connection.Execute(
            "UPDATE [ReSaleItem] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [Deleted] = 1 " +
            "WHERE [ReSaleID] = @Id; ",
            new { Id = reSaleId });
    }
}