using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.EntityHelpers.Charts;
using GBA.Domain.Repositories.Charts.Contracts;

namespace GBA.Domain.Repositories.Charts;

public sealed class ExchangeRateChartsRepository : IExchangeRateChartsRepository {
    private readonly IDbConnection _connection;

    public ExchangeRateChartsRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<ForChartExchangeRate> GetForUkrainianExchangeRatesRanged(DateTime from, DateTime to) {
        List<ForChartExchangeRate> exchangeRates =
            _connection.Query<ExchangeRate>(
                "SELECT * " +
                "FROM [ExchangeRate] " +
                "WHERE [ExchangeRate].Deleted = 0 " +
                "AND [ExchangeRate].Culture = 'uk'"
            ).Select(exchangeRate => new ForChartExchangeRate(exchangeRate)).ToList();

        foreach (ForChartExchangeRate exchangeRate in exchangeRates)
            exchangeRate.ExchangeRateValues =
                _connection.Query<ForChartExchangeRateValue>(
                    "SELECT [ExchangeRateHistory].Created " +
                    ",[ExchangeRateHistory].Amount AS [Value] " +
                    "FROM [ExchangeRateHistory] " +
                    "WHERE [ExchangeRateHistory].ExchangeRateID = @Id " +
                    "AND [ExchangeRateHistory].Created >= @From " +
                    "AND [ExchangeRateHistory].Created <= @To " +
                    "ORDER BY [ExchangeRateHistory].Created",
                    new { From = from, To = to, exchangeRate.ExchangeRate.Id }
                ).ToList();

        return exchangeRates;
    }

    public IEnumerable<ForChartExchangeRate> GetForPolandExchangeRatesRanged(DateTime from, DateTime to) {
        List<ForChartExchangeRate> exchangeRates =
            _connection.Query<ExchangeRate>(
                "SELECT * " +
                "FROM [ExchangeRate] " +
                "WHERE [ExchangeRate].Deleted = 0 " +
                "AND [ExchangeRate].Culture = 'pl'"
            ).Select(exchangeRate => new ForChartExchangeRate(exchangeRate)).ToList();

        foreach (ForChartExchangeRate exchangeRate in exchangeRates)
            exchangeRate.ExchangeRateValues =
                _connection.Query<ForChartExchangeRateValue>(
                    "SELECT [ExchangeRateHistory].Created " +
                    ",[ExchangeRateHistory].Amount AS [Value] " +
                    "FROM [ExchangeRateHistory] " +
                    "WHERE [ExchangeRateHistory].ExchangeRateID = @Id " +
                    "AND [ExchangeRateHistory].Created >= @From " +
                    "AND [ExchangeRateHistory].Created <= @To " +
                    "ORDER BY [ExchangeRateHistory].Created",
                    new { From = from, To = to, exchangeRate.ExchangeRate.Id }
                ).ToList();

        return exchangeRates;
    }

    public IEnumerable<ForChartExchangeRate> GetCrossExchangeRatesRanged(DateTime from, DateTime to) {
        List<ForChartExchangeRate> exchangeRates =
            _connection.Query<CrossExchangeRate>(
                "SELECT * " +
                "FROM [CrossExchangeRate] " +
                "WHERE [CrossExchangeRate].Deleted = 0"
            ).Select(exchangeRate => new ForChartExchangeRate(exchangeRate)).ToList();

        foreach (ForChartExchangeRate exchangeRate in exchangeRates)
            exchangeRate.ExchangeRateValues =
                _connection.Query<ForChartExchangeRateValue>(
                    "SELECT [CrossExchangeRateHistory].Created " +
                    ",[CrossExchangeRateHistory].Amount AS [Value] " +
                    "FROM [CrossExchangeRateHistory] " +
                    "WHERE [CrossExchangeRateHistory].CrossExchangeRateID = @Id " +
                    "AND [CrossExchangeRateHistory].Created >= @From " +
                    "AND [CrossExchangeRateHistory].Created <= @To " +
                    "ORDER BY [CrossExchangeRateHistory].Created",
                    new { From = from, To = to, exchangeRate.CrossExchangeRate.Id }
                ).ToList();

        return exchangeRates;
    }
}