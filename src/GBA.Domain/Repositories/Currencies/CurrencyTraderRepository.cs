using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.Currencies.Contracts;

namespace GBA.Domain.Repositories.Currencies;

public sealed class CurrencyTraderRepository : ICurrencyTraderRepository {
    private readonly IDbConnection _connection;

    public CurrencyTraderRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(CurrencyTrader currencyTrader) {
        return _connection.Query<long>(
                "INSERT INTO [CurrencyTrader] (FirstName, LastName, MiddleName, PhoneNumber, Updated) " +
                "VALUES (@FirstName, @LastName, @MiddleName, @PhoneNumber, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                currencyTrader
            )
            .Single();
    }

    public void Update(CurrencyTrader currencyTrader) {
        _connection.Execute(
            "UPDATE [CurrencyTrader] " +
            "SET FirstName = @FirstName, LastName = @LastName, MiddleName = @MiddleName, PhoneNumber = @PhoneNumber, Updated = getutcdate() " +
            "WHERE [CurrencyTrader].ID = @Id",
            currencyTrader
        );
    }

    public CurrencyTrader GetById(long id) {
        return _connection.Query<CurrencyTrader>(
                "SELECT * " +
                "FROM [CurrencyTrader] " +
                "WHERE [CurrencyTrader].ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public CurrencyTrader GetByNetId(Guid netId) {
        return _connection.Query<CurrencyTrader>(
                "SELECT * " +
                "FROM [CurrencyTrader] " +
                "WHERE [CurrencyTrader].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public IEnumerable<CurrencyTrader> GetAll() {
        return _connection.Query<CurrencyTrader>(
            "SELECT * " +
            "FROM [CurrencyTrader] " +
            "WHERE [CurrencyTrader].Deleted = 0"
        );
    }

    public IEnumerable<CurrencyTrader> FindByPaymentCurrencyRegisterNetId(string currencyCode) {
        List<CurrencyTrader> toReturn = new();

        _connection.Query<CurrencyTrader, CurrencyTraderExchangeRate, CurrencyTrader>(
            "SELECT * " +
            "FROM [CurrencyTrader] " +
            "LEFT JOIN [CurrencyTraderExchangeRate] " +
            "ON [CurrencyTraderExchangeRate].CurrencyTraderID = [CurrencyTrader].ID " +
            "AND [CurrencyTraderExchangeRate].FromDate >= CONVERT(date, GETUTCDATE()) " +
            "AND [CurrencyTraderExchangeRate].Deleted = 0 " +
            "WHERE [CurrencyTrader].ID IN (" +
            "SELECT [CurrencyTrader].ID " +
            "FROM [CurrencyTrader] " +
            "LEFT JOIN [CurrencyTraderExchangeRate] " +
            "ON [CurrencyTraderExchangeRate].CurrencyTraderID = [CurrencyTrader].ID " +
            "AND [CurrencyTraderExchangeRate].FromDate >= CONVERT(date, GETUTCDATE()) " +
            "WHERE [CurrencyTraderExchangeRate].CurrencyName = @CurrencyCode " +
            "AND [CurrencyTrader].Deleted = 0 " +
            "AND [CurrencyTraderExchangeRate].Deleted = 0 " +
            "GROUP BY [CurrencyTrader].ID" +
            ") " +
            "AND [CurrencyTraderExchangeRate].ID IS NOT NULL " +
            "ORDER BY [CurrencyTraderExchangeRate].FromDate DESC",
            (trader, exchangeRate) => {
                if (!toReturn.Any(t => t.Id.Equals(trader.Id))) {
                    trader.CurrencyTraderExchangeRates.Add(exchangeRate);

                    toReturn.Add(trader);
                } else {
                    CurrencyTrader currencyTrader = toReturn.First(t => t.Id.Equals(trader.Id));

                    if (!currencyTrader.CurrencyTraderExchangeRates.Any(r => r.CurrencyName.Equals(exchangeRate.CurrencyName)))
                        currencyTrader.CurrencyTraderExchangeRates.Add(exchangeRate);
                }

                return trader;
            },
            new { CurrencyCode = currencyCode }
        );

        return toReturn;
    }

    public IEnumerable<CurrencyTrader> GetAllFromSearch(string value) {
        return _connection.Query<CurrencyTrader>(
            "SELECT [CurrencyTrader].* " +
            "FROM [CurrencyTrader] " +
            "WHERE [CurrencyTrader].Deleted = 0 " +
            "AND (" +
            "(ISNULL([CurrencyTrader].FirstName, '') + ' ' " +
            "+ ISNULL([CurrencyTrader].MiddleName, '') + ' ' " +
            "+ ISNULL([CurrencyTrader].LastName, '') + ' ' " +
            "+ ISNULL([CurrencyTrader].PhoneNumber, '')) like @Value " +
            "OR " +
            "(ISNULL([CurrencyTrader].PhoneNumber, '') + ' ' " +
            "+ ISNULL([CurrencyTrader].FirstName, '') + ' ' " +
            "+ ISNULL([CurrencyTrader].MiddleName, '') + ' ' " +
            "+ ISNULL([CurrencyTrader].LastName, '')) like @Value" +
            ")",
            new { Value = value }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [CurrencyTrader] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [CurrencyTrader].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}