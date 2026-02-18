using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Repositories.ExchangeRates.Contracts;

namespace GBA.Domain.Repositories.ExchangeRates;

public sealed class GovCrossExchangeRateRepository : IGovCrossExchangeRateRepository {
    private readonly IDbConnection _connection;

    public GovCrossExchangeRateRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<GovCrossExchangeRate> GetAll() {
        return _connection.Query<GovCrossExchangeRate>(
            "SELECT * " +
            "FROM [GovCrossExchangeRate] " +
            "WHERE Deleted = 0 " +
            "ORDER BY Code"
        ).ToList();
    }

    public GovCrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId) {
        return _connection.Query<GovCrossExchangeRate>(
            "SELECT * " +
            "FROM [GovCrossExchangeRate] " +
            "WHERE CurrencyFromID = @FromId " +
            "AND CurrencyToID = @ToId " +
            "AND Deleted = 0",
            new { FromId = currencyFromId, ToId = currencyToId }
        ).FirstOrDefault();
    }

    public GovCrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId, DateTime fromDate) {
        return _connection.Query<GovCrossExchangeRate>(
            "SELECT TOP(1) " +
            "[GovCrossExchangeRate].ID, " +
            "(CASE " +
            "WHEN [GovCrossExchangeRateHistory].Amount IS NOT NULL " +
            "THEN [GovCrossExchangeRateHistory].Amount " +
            "ELSE [GovCrossExchangeRate].Amount " +
            "END) AS [Amount] " +
            "FROM [GovCrossExchangeRate] " +
            "LEFT JOIN [GovCrossExchangeRateHistory] " +
            "ON [GovCrossExchangeRateHistory].GovCrossExchangeRateID = [GovCrossExchangeRate].ID " +
            "AND [GovCrossExchangeRate].Created <= @FromDate " +
            "WHERE [GovCrossExchangeRate].CurrencyFromID = @FromId " +
            "AND [GovCrossExchangeRate].CurrencyToID = @ToId " +
            "AND [GovCrossExchangeRate].Deleted = 0 " +
            "ORDER BY [GovCrossExchangeRateHistory].Created DESC",
            new { FromId = currencyFromId, ToId = currencyToId, FromDate = fromDate }
        ).FirstOrDefault();
    }

    public GovCrossExchangeRate GetByNetId(Guid netId) {
        return _connection.Query<GovCrossExchangeRate>(
            "SELECT * " +
            "FROM [GovCrossExchangeRate] " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public void Update(GovCrossExchangeRate govCrossExchangeRate) {
        _connection.Execute(
            "UPDATE [GovCrossExchangeRate] " +
            "SET Amount = @Amount, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            govCrossExchangeRate
        );
    }
}