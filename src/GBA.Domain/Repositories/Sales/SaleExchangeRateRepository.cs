using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class SaleExchangeRateRepository : ISaleExchangeRateRepository {
    private readonly IDbConnection _connection;

    public SaleExchangeRateRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(SaleExchangeRate saleExchangeRate) {
        _connection.Execute(
            "INSERT INTO SaleExchangeRate (ExchangeRateId, SaleId, Value, Updated) " +
            "VALUES (@ExchangeRateId, @SaleId, @Value, getutcdate())",
            saleExchangeRate
        );
    }

    public void Add(IEnumerable<SaleExchangeRate> saleExchangeRates) {
        _connection.Execute(
            "INSERT INTO SaleExchangeRate (ExchangeRateId, SaleId, Value, Updated) " +
            "VALUES (@ExchangeRateId, @SaleId, @Value, getutcdate())",
            saleExchangeRates
        );
    }

    public SaleExchangeRate GetEuroSaleExchangeRateBySaleNetId(Guid netId) {
        return _connection.Query<SaleExchangeRate, ExchangeRate, SaleExchangeRate>(
                "SELECT * FROM SaleExchangeRate " +
                "LEFT OUTER JOIN ExchangeRate " +
                "ON ExchangeRate.ID = SaleExchangeRate.ExchangeRateID AND ExchangeRate.Deleted = 0 " +
                "LEFT OUTER JOIN Sale " +
                "ON Sale.ID = SaleExchangeRate.SaleID AND Sale.Deleted = 0 " +
                "WHERE Sale.NetUID = @NetId " +
                "AND ExchangeRate.Code = 'EUR'",
                (saleExchangeRate, exchangeRate) => {
                    saleExchangeRate.ExchangeRate = exchangeRate;

                    return saleExchangeRate;
                },
                new { NetId = netId.ToString() }
            )
            .FirstOrDefault();
    }

    public List<SaleExchangeRate> GetAllBySaleNetId(Guid saleNetId) {
        return _connection.Query<SaleExchangeRate, ExchangeRate, Sale, SaleExchangeRate>(
                "SELECT * FROM SaleExchangeRate " +
                "LEFT OUTER JOIN ExchangeRate " +
                "ON ExchangeRate.ID = SaleExchangeRate.ExchangeRateID AND ExchangeRate.Deleted = 0 " +
                "LEFT OUTER JOIN Sale " +
                "ON Sale.ID = SaleExchangeRate.SaleID AND Sale.Deleted = 0 " +
                "WHERE Sale.NetUID = @SaleNetId",
                (saleExchangeRate, exchangeRate, sale) => {
                    if (exchangeRate != null) saleExchangeRate.ExchangeRate = exchangeRate;

                    return saleExchangeRate;
                },
                new { SaleNetId = saleNetId }
            )
            .ToList();
    }

    public void Remove(SaleExchangeRate saleExchangeRate) {
        _connection.Execute(
            "UPDATE SaleExchangeRate SET Deleted = 1 WHERE NetUid = @NetUid",
            saleExchangeRate
        );
    }

    public void Remove(IEnumerable<SaleExchangeRate> saleExchangeRates) {
        _connection.Execute(
            "UPDATE SaleExchangeRate SET Deleted = 1 WHERE NetUid = @NetUid",
            saleExchangeRates
        );
    }

    public void Update(SaleExchangeRate saleExchangeRate) {
        _connection.Execute(
            "UPDATE SaleExchangeRate " +
            "SET ExchangeRateId = @ExchangeRateId, SaleId = @SaleId, Value = @Value, Updated = getutcdate() " +
            "WHERE NetUid = @NetUid",
            saleExchangeRate
        );
    }

    public void Update(IEnumerable<SaleExchangeRate> saleExchangeRates) {
        _connection.Execute(
            "UPDATE SaleExchangeRate " +
            "SET ExchangeRateId = @ExchangeRateId, SaleId = @SaleId, Value = @Value, Updated = getutcdate() " +
            "WHERE NetUid = @NetUid",
            saleExchangeRates
        );
    }

    public SaleExchangeRate GetEuroSaleExchangeRateBySaleIdForCurrentCulture(long saleId) {
        return _connection.Query<SaleExchangeRate, ExchangeRate, SaleExchangeRate>(
                "SELECT TOP(1) * " +
                "FROM [SaleExchangeRate] " +
                "LEFT JOIN [ExchangeRate] " +
                "ON [SaleExchangeRate].ExchangeRateID = [ExchangeRate].ID " +
                "WHERE [ExchangeRate].Deleted = 0 " +
                "AND [ExchangeRate].Culture = @Culture " +
                "AND [ExchangeRate].Code = 'EUR' " +
                "AND [SaleExchangeRate].SaleID = @SaleId",
                (saleExchangeRate, exchangeRate) => {
                    saleExchangeRate.ExchangeRate = exchangeRate;

                    return saleExchangeRate;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, SaleId = saleId }
            )
            .SingleOrDefault();
    }
}