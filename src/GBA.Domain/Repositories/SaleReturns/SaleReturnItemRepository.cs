using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Repositories.SaleReturns.Contracts;

namespace GBA.Domain.Repositories.SaleReturns;

public sealed class SaleReturnItemRepository : ISaleReturnItemRepository {
    private readonly IDbConnection _connection;

    public SaleReturnItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SaleReturnItem saleReturnItem) {
        return _connection.Query<long>(
            "INSERT INTO [SaleReturnItem] (Qty, SaleReturnItemStatus, IsMoneyReturned, OrderItemId, SaleReturnId, CreatedById, UpdatedById, MoneyReturnedById, " +
            "MoneyReturnedAt, StorageId, Amount, ExchangeRateAmount, Updated) " +
            "VALUES (@Qty, @SaleReturnItemStatus, @IsMoneyReturned, @OrderItemId, @SaleReturnId, @CreatedById, @UpdatedById, @MoneyReturnedById, @MoneyReturnedAt, " +
            "@StorageId, @Amount, @ExchangeRateAmount, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY(); ",
            saleReturnItem
        ).FirstOrDefault();
    }

    public void Add(IEnumerable<SaleReturnItem> saleReturnItems) {
        _connection.Execute(
            "INSERT INTO [SaleReturnItem] (Qty, SaleReturnItemStatus, IsMoneyReturned, OrderItemId, SaleReturnId, CreatedById, UpdatedById, MoneyReturnedById, " +
            "MoneyReturnedAt, StorageId, Updated) " +
            "VALUES (@Qty, @SaleReturnItemStatus, @IsMoneyReturned, @OrderItemId, @SaleReturnId, @CreatedById, @UpdatedById, @MoneyReturnedById, @MoneyReturnedAt, " +
            "@StorageId, GETUTCDATE())",
            saleReturnItems
        );
    }

    public void Update(IEnumerable<SaleReturnItem> saleReturnItems) {
        _connection.Execute(
            "UPDATE [SaleReturnItem] " +
            "SET ExchangeRateAmount = @ExchangeRateAmount, Amount = @Amount, StorageID = @StorageId " +
            "WHERE ID = @Id",
            saleReturnItems
        );
    }

    public void Update(SaleReturnItem saleReturnItem) {
        _connection.Execute(
            "UPDATE [SaleReturnItem] SET Qty = @Qty, " +
            "StorageId = @StorageId, Amount = @Amount, Updated = GETUTCDATE() " +
            "WHERE ID = @Id ",
            saleReturnItem
        );
    }

    public void UpdateProductPlacement(SaleReturnItem saleReturnItem) {
        _connection.Execute(
            "UPDATE [SaleReturnItem] " +
            "SET ProductPlacementId = @ProductPlacementId " +
            "WHERE ID = @Id",
            saleReturnItem
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids, long updatedById) {
        _connection.Execute(
            "UPDATE [SaleReturnItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE(), UpdatedById = @UpdatedById " +
            "WHERE ID IN @Ids",
            new { Ids = ids, UpdatedById = updatedById }
        );
    }

    public void RemoveById(long id) {
        _connection.Execute(
            "UPDATE [SaleReturnItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id ",
            new { Id = id }
        );
    }
}