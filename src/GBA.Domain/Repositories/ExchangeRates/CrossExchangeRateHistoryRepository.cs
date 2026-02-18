using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Repositories.ExchangeRates.Contracts;

namespace GBA.Domain.Repositories.ExchangeRates;

public sealed class CrossExchangeRateHistoryRepository : ICrossExchangeRateHistoryRepository {
    private readonly IDbConnection _connection;

    public CrossExchangeRateHistoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(CrossExchangeRateHistory crossExchangeRateHistory) {
        return _connection.Query<long>(
            "INSERT INTO [CrossExchangeRateHistory] (Amount, UpdatedById, CrossExchangeRateId, Updated) " +
            "VALUES (@Amount, @UpdatedById, @CrossExchangeRateId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            crossExchangeRateHistory
        ).Single();
    }

    public long AddSpecific(CrossExchangeRateHistory crossExchangeRateHistory) {
        return _connection.Query<long>(
            "INSERT INTO [CrossExchangeRateHistory] (Amount, UpdatedById, CrossExchangeRateId, Created, Updated) " +
            "VALUES (@Amount, @UpdatedById, @CrossExchangeRateId, @Created, @Updated); " +
            "SELECT SCOPE_IDENTITY()",
            crossExchangeRateHistory
        ).Single();
    }

    public List<CrossExchangeRateHistory> GetAllByCrossExchangeRateNetId(Guid netId, long limit, long offset) {
        return _connection.Query<CrossExchangeRateHistory, User, CrossExchangeRateHistory>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [CrossExchangeRateHistory].Created DESC) AS RowNumber " +
            ", [CrossExchangeRateHistory].ID " +
            "FROM [CrossExchangeRateHistory] " +
            "LEFT JOIN [CrossExchangeRate] " +
            "ON [CrossExchangeRate].ID = [CrossExchangeRateHistory].CrossExchangeRateID " +
            "WHERE [CrossExchangeRate].NetUID = @NetId " +
            ") " +
            "SELECT * " +
            "FROM [CrossExchangeRateHistory] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [CrossExchangeRateHistory].UpdatedByID " +
            "WHERE [CrossExchangeRateHistory].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")",
            (history, user) => {
                history.UpdatedBy = user;

                return history;
            },
            new { NetId = netId, Limit = limit, Offset = offset }
        ).ToList();
    }

    public IEnumerable<CrossExchangeRate> GetAllByCrossExchangeRateNetIds(IEnumerable<Guid> netIds, long limit, long offset, DateTime from, DateTime to) {
        IEnumerable<CrossExchangeRate> crossExchangeRates =
            _connection.Query<CrossExchangeRate, Currency, Currency, CrossExchangeRate>(
                "SELECT * " +
                "FROM [CrossExchangeRate] " +
                "LEFT JOIN [views].[CurrencyView] AS [CurrencyFrom] " +
                "ON [CurrencyFrom].ID = [CrossExchangeRate].CurrencyFromID " +
                "AND [CurrencyFrom].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [CurrencyTo] " +
                "ON [CurrencyTo].ID = [CrossExchangeRate].CurrencyToID " +
                "AND [CurrencyTo].CultureCode = @Culture " +
                "WHERE [CrossExchangeRate].NetUID IN @NetIds ",
                (crossExchangeRate, currencyFrom, currencyTo) => {
                    crossExchangeRate.CurrencyFrom = currencyFrom;
                    crossExchangeRate.CurrencyTo = currencyTo;

                    return crossExchangeRate;
                },
                new { NetIds = netIds, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

        foreach (CrossExchangeRate crossExchangeRate in crossExchangeRates)
            crossExchangeRate.CrossExchangeRateHistories =
                _connection.Query<CrossExchangeRateHistory, User, CrossExchangeRateHistory>(
                    ";WITH [Search_CTE] " +
                    "AS " +
                    "( " +
                    "SELECT ROW_NUMBER() OVER (ORDER BY [CrossExchangeRateHistory].Created) AS RowNumber " +
                    ", [CrossExchangeRateHistory].ID " +
                    "FROM [CrossExchangeRateHistory] " +
                    "WHERE [CrossExchangeRateHistory].CrossExchangeRateID = @Id " +
                    "AND [CrossExchangeRateHistory].Created >= @From " +
                    "AND [CrossExchangeRateHistory].Created <= @To " +
                    ") " +
                    "SELECT * " +
                    "FROM [CrossExchangeRateHistory] " +
                    "LEFT JOIN [User] " +
                    "ON [User].ID = [CrossExchangeRateHistory].UpdatedByID " +
                    "WHERE [CrossExchangeRateHistory].ID IN ( " +
                    "SELECT [Search_CTE].ID " +
                    "FROM [Search_CTE] " +
                    "WHERE [Search_CTE].RowNumber > @Offset " +
                    "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
                    ")",
                    (history, user) => {
                        history.UpdatedBy = user;

                        return history;
                    },
                    new { crossExchangeRate.Id, Limit = limit, Offset = offset, From = from, To = to }
                ).ToList();

        return crossExchangeRates;
    }

    public CrossExchangeRateHistory GetLatestByCrossExchangeRateNetId(Guid netId) {
        return _connection.Query<CrossExchangeRateHistory>(
            "SELECT TOP(1) [CrossExchangeRateHistory].* " +
            "FROM [CrossExchangeRateHistory] " +
            "LEFT JOIN [CrossExchangeRate] " +
            "ON [CrossExchangeRate].ID = [CrossExchangeRateHistory].CrossExchangeRateID " +
            "WHERE [CrossExchangeRate].NetUID = @NetId " +
            "ORDER BY [CrossExchangeRateHistory].Created",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public CrossExchangeRateHistory GetLatestNearToDateByCrossExchangeRateNetId(Guid netId, DateTime fromDate) {
        return _connection.Query<CrossExchangeRateHistory>(
            "SELECT TOP(1) [CrossExchangeRateHistory].* " +
            "FROM [CrossExchangeRateHistory] " +
            "LEFT JOIN [CrossExchangeRate] " +
            "ON [CrossExchangeRate].ID = [CrossExchangeRateHistory].CrossExchangeRateID " +
            "WHERE [CrossExchangeRate].NetUID = @NetId " +
            "AND [CrossExchangeRateHistory].Created <= @FromDate " +
            "ORDER BY [CrossExchangeRateHistory].Created DESC",
            new { NetId = netId, FromDate = fromDate }
        ).SingleOrDefault();
    }

    public CrossExchangeRateHistory GetFirstByCrossExchangeRateId(long id) {
        return _connection.Query<CrossExchangeRateHistory>(
            "SELECT TOP(1) [CrossExchangeRateHistory].* " +
            "FROM [CrossExchangeRateHistory] " +
            "WHERE [CrossExchangeRateHistory].CrossExchangeRateID = @Id " +
            "ORDER BY [CrossExchangeRateHistory].Created DESC ",
            new { Id = id }
        ).SingleOrDefault();
    }
}