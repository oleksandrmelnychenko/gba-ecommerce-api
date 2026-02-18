using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Repositories.ExchangeRates.Contracts;

namespace GBA.Domain.Repositories.ExchangeRates;

public class CrossExchangeRateRepository : ICrossExchangeRateRepository {
    private readonly IDbConnection _connection;

    public CrossExchangeRateRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<CrossExchangeRate> GetAll() {
        return _connection.Query<CrossExchangeRate>(
            "SELECT * " +
            "FROM CrossExchangeRate " +
            "WHERE Deleted = 0 " +
            "ORDER BY Code"
        ).ToList();
    }

    public CrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId) {
        return _connection.Query<CrossExchangeRate>(
            "SELECT * FROM CrossExchangeRate " +
            "WHERE CurrencyFromID = @FromId " +
            "AND CurrencyToID = @ToId " +
            "AND Deleted = 0",
            new { FromId = currencyFromId, ToId = currencyToId }
        ).FirstOrDefault();
    }

    public CrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId, DateTime fromDate) {
        return _connection.Query<CrossExchangeRate>(
            "SELECT TOP(1) " +
            "[CrossExchangeRate].ID, " +
            "(CASE " +
            "WHEN [CrossExchangeRateHistory].Amount IS NOT NULL " +
            "THEN [CrossExchangeRateHistory].Amount " +
            "ELSE [CrossExchangeRate].Amount " +
            "END) AS [Amount] " +
            "FROM [CrossExchangeRate] " +
            "LEFT JOIN [CrossExchangeRateHistory] " +
            "ON [CrossExchangeRateHistory].CrossExchangeRateID = [CrossExchangeRate].ID " +
            "AND [CrossExchangeRate].Created <= @FromDate " +
            "WHERE [CrossExchangeRate].CurrencyFromID = @FromId " +
            "AND [CrossExchangeRate].CurrencyToID = @ToId " +
            "AND [CrossExchangeRate].Deleted = 0 " +
            "ORDER BY [CrossExchangeRate].Created DESC",
            new { FromId = currencyFromId, ToId = currencyToId, FromDate = fromDate }
        ).FirstOrDefault();
    }

    public CrossExchangeRate GetByNetId(Guid netId) {
        return _connection.Query<CrossExchangeRate>(
            "SELECT * FROM CrossExchangeRate WHERE NetUID = @NetId",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public void Update(CrossExchangeRate crossExchangeRate) {
        _connection.Execute(
            "UPDATE CrossExchangeRate SET Amount = @Amount, Updated = getutcdate() WHERE NetUID = @NetUID",
            crossExchangeRate
        );
    }
}