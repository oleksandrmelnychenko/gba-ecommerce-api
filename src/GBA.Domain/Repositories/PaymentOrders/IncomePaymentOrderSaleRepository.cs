using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class IncomePaymentOrderSaleRepository : IIncomePaymentOrderSaleRepository {
    private readonly IDbConnection _connection;

    public IncomePaymentOrderSaleRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IncomePaymentOrderSale incomePaymentOrderSale) {
        _connection.Execute(
            "INSERT INTO [IncomePaymentOrderSale] (SaleId, IncomePaymentOrderId, Amount, OverpaidAmount, ExchangeRate, Updated, [ReSaleId]) " +
            "VALUES (@SaleId, @IncomePaymentOrderId, @Amount, @OverpaidAmount, @ExchangeRate, getutcdate(), @ReSaleId)",
            incomePaymentOrderSale
        );
    }

    public void Add(IEnumerable<IncomePaymentOrderSale> incomePaymentOrderSales) {
        _connection.Execute(
            "INSERT INTO [IncomePaymentOrderSale] (SaleId, IncomePaymentOrderId, Amount, OverpaidAmount, ExchangeRate, Updated, [ReSaleId]) " +
            "VALUES (@SaleId, @IncomePaymentOrderId, @Amount, @OverpaidAmount, @ExchangeRate, getutcdate(), @ReSaleId)",
            incomePaymentOrderSales
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [IncomePaymentOrderSale] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [IncomePaymentOrderSale].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [IncomePaymentOrderSale] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [IncomePaymentOrderSale].ID = @Id",
            new { Id = id }
        );
    }

    public void UpdateAmount(IncomePaymentOrderSale incomePaymentOrderSale) {
        _connection.Execute(
            "UPDATE [IncomePaymentOrderSale] " +
            "SET Amount = @Amount, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            incomePaymentOrderSale
        );
    }

    public bool CheckIsMoreThanOnePaymentBySaleId(long saleId) {
        return _connection.Query<bool>(
                "SELECT CASE WHEN COUNT(1) > 1 THEN 1 ELSE 0 END " +
                "FROM [IncomePaymentOrderSale] " +
                "WHERE [IncomePaymentOrderSale].SaleID = @SaleId " +
                "AND [IncomePaymentOrderSale].Deleted = 0",
                new { SaleId = saleId }
            )
            .Single();
    }
}