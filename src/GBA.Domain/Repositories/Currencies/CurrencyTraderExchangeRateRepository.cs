using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.Currencies.Contracts;

namespace GBA.Domain.Repositories.Currencies;

public sealed class CurrencyTraderExchangeRateRepository : ICurrencyTraderExchangeRateRepository {
    private readonly IDbConnection _connection;

    public CurrencyTraderExchangeRateRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(CurrencyTraderExchangeRate currencyTraderExchangeRate) {
        return _connection.Query<long>(
                "INSERT INTO [CurrencyTraderExchangeRate] (CurrencyName, ExchangeRate, CurrencyTraderId, FromDate, Updated) " +
                "VALUES (@CurrencyName, @ExchangeRate, @CurrencyTraderId, @FromDate, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                currencyTraderExchangeRate
            )
            .Single();
    }

    public void Add(IEnumerable<CurrencyTraderExchangeRate> currencyTraderExchangeRates) {
        _connection.Execute(
            "INSERT INTO [CurrencyTraderExchangeRate] (CurrencyName, ExchangeRate, CurrencyTraderId, FromDate, Updated) " +
            "VALUES (@CurrencyName, @ExchangeRate, @CurrencyTraderId, @FromDate, getutcdate())",
            currencyTraderExchangeRates
        );
    }

    public void Update(CurrencyTraderExchangeRate currencyTraderExchangeRate) {
        _connection.Execute(
            "UPDATE [CurrencyTraderExchangeRate] " +
            "SET CurrencyName = @CurrencyName, ExchangeRate = @ExchangeRate, CurrencyTraderId = @CurrencyTraderId, FromDate = @FromDate, Updated = getutcdate() " +
            "WHERE [CurrencyTraderExchangeRate].ID = @Id",
            currencyTraderExchangeRate
        );
    }

    public void Update(IEnumerable<CurrencyTraderExchangeRate> currencyTraderExchangeRates) {
        _connection.Execute(
            "UPDATE [CurrencyTraderExchangeRate] " +
            "SET CurrencyName = @CurrencyName, ExchangeRate = @ExchangeRate, CurrencyTraderId = @CurrencyTraderId, FromDate = @FromDate, Updated = getutcdate() " +
            "WHERE [CurrencyTraderExchangeRate].ID = @Id",
            currencyTraderExchangeRates
        );
    }

    public CurrencyTraderExchangeRate GetByNetId(Guid netId) {
        return _connection.Query<CurrencyTraderExchangeRate>(
                "SELECT * " +
                "FROM [CurrencyTraderExchangeRate] " +
                "WHERE [CurrencyTraderExchangeRate].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public IEnumerable<CurrencyTraderExchangeRate> GetAllByTraderIdFiltered(long id, DateTime from, DateTime to) {
        return _connection.Query<CurrencyTraderExchangeRate>(
            "SELECT * " +
            "FROM [CurrencyTraderExchangeRate] " +
            "WHERE [CurrencyTraderExchangeRate].CurrencyTraderID = @Id " +
            "AND [CurrencyTraderExchangeRate].Deleted = 0 " +
            "AND [CurrencyTraderExchangeRate].FromDate >= @From " +
            "AND [CurrencyTraderExchangeRate].FromDate < @To " +
            "ORDER BY [CurrencyTraderExchangeRate].FromDate DESC, " +
            "[CurrencyTraderExchangeRate].[CurrencyName] DESC",
            new { Id = id, From = from, To = to }
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [CurrencyTraderExchangeRate] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [CurrencyTraderExchangeRate].ID = @Id",
            new { Id = id }
        );
    }

    public void RemoveByFromDateAndTraderId(DateTime fromDate, long id) {
        _connection.Execute(
            "UPDATE [CurrencyTraderExchangeRate] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [CurrencyTraderExchangeRate].CurrencyTraderID = @Id " +
            "AND [CurrencyTraderExchangeRate].FromDate = @FromDate",
            new { FromDate = fromDate, Id = id }
        );
    }

    public void Remove(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [CurrencyTraderExchangeRate] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [CurrencyTraderExchangeRate].ID IN @Ids",
            new { Ids = ids }
        );
    }
}