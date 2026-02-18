using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.EntityHelpers.Charts;
using GBA.Domain.Repositories.Charts.Contracts;

namespace GBA.Domain.Repositories.Charts;

public sealed class CurrencyTraderExchangeRateChartsRepository : ICurrencyTraderExchangeRateChartsRepository {
    private readonly IDbConnection _connection;

    public CurrencyTraderExchangeRateChartsRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<ForChartCurrencyTrader> GetCurrencyTraderExchangeRatesFiltered(
        DateTime from,
        DateTime to,
        IEnumerable<Guid> traderNetIds,
        IEnumerable<string> currencies
    ) {
        string tradersSqlExpression = "SELECT * " +
                                      "FROM [CurrencyTrader] " +
                                      "WHERE [CurrencyTrader].Deleted = 0 ";

        if (traderNetIds.Any()) tradersSqlExpression += "AND [CurrencyTrader].NetUID IN @NetIds ";

        tradersSqlExpression += "ORDER BY [CurrencyTrader].LastName";

        List<ForChartCurrencyTrader> traders =
            _connection.Query<CurrencyTrader>(
                tradersSqlExpression,
                new { NetIds = traderNetIds }
            ).Select(trader => new ForChartCurrencyTrader(trader)).ToList();

        string exchangeRatesSqlExpression = "SELECT * " +
                                            "FROM [CurrencyTraderExchangeRate] " +
                                            "LEFT JOIN [CurrencyTrader] " +
                                            "ON [CurrencyTraderExchangeRate].CurrencyTraderID = [CurrencyTrader].ID " +
                                            "WHERE [CurrencyTraderExchangeRate].Deleted = 0 " +
                                            "AND [CurrencyTraderExchangeRate].FromDate >= @From " +
                                            "AND [CurrencyTraderExchangeRate].FromDate <= @to " +
                                            "AND [CurrencyTrader].NetUID IN @NetIds ";

        if (currencies.Any()) exchangeRatesSqlExpression += "AND [CurrencyTraderExchangeRate].CurrencyName IN @Currencies ";

        exchangeRatesSqlExpression += "ORDER BY [CurrencyTraderExchangeRate].FromDate";

        _connection.Query<CurrencyTraderExchangeRate, CurrencyTrader, CurrencyTraderExchangeRate>(
            exchangeRatesSqlExpression,
            (exchangeRate, trader) => {
                ForChartCurrencyTrader traderFromList = traders.First(t => t.CurrencyTrader.Id.Equals(trader.Id));

                if (traderFromList.Currencies.Any(c => c.CurrencyName.Equals(exchangeRate.CurrencyName))) {
                    traderFromList
                        .Currencies
                        .First(c => c.CurrencyName.Equals(exchangeRate.CurrencyName))
                        .Values
                        .Add(new ForChartCurrencyTraderCurrencyValue {
                            Created = exchangeRate.FromDate,
                            Value = exchangeRate.ExchangeRate
                        });
                } else {
                    ForChartCurrencyTraderCurrency currency = new(exchangeRate.CurrencyName);

                    currency
                        .Values
                        .Add(new ForChartCurrencyTraderCurrencyValue {
                            Created = exchangeRate.FromDate,
                            Value = exchangeRate.ExchangeRate
                        });

                    traderFromList
                        .Currencies
                        .Add(currency);
                }

                return exchangeRate;
            },
            new { NetIds = traders.Select(t => t.CurrencyTrader.NetUid), Currencies = currencies, From = from, To = to }
        );

        return traders;
    }
}