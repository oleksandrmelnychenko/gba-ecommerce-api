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

public sealed class GovCrossExchangeRateHistoryRepository : IGovCrossExchangeRateHistoryRepository {
    private readonly IDbConnection _connection;

    public GovCrossExchangeRateHistoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(GovCrossExchangeRateHistory crossExchangeRateHistory) {
        return _connection.Query<long>(
            "INSERT INTO [GovCrossExchangeRateHistory] (Amount, UpdatedById, GovCrossExchangeRateId, Updated) " +
            "VALUES (@Amount, @UpdatedById, @GovCrossExchangeRateId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            crossExchangeRateHistory
        ).Single();
    }

    public long AddSpecific(GovCrossExchangeRateHistory crossExchangeRateHistory) {
        return _connection.Query<long>(
            "INSERT INTO [GovCrossExchangeRateHistory] (Amount, UpdatedById, GovCrossExchangeRateId, Created, Updated) " +
            "VALUES (@Amount, @UpdatedById, @GovCrossExchangeRateId, @Created, @Updated); " +
            "SELECT SCOPE_IDENTITY()",
            crossExchangeRateHistory
        ).Single();
    }

    public List<GovCrossExchangeRateHistory> GetAllByGovCrossExchangeRateNetId(Guid netId, long limit, long offset) {
        return _connection.Query<GovCrossExchangeRateHistory, User, GovCrossExchangeRateHistory>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [GovCrossExchangeRateHistory].Created DESC) AS RowNumber " +
            ", [GovCrossExchangeRateHistory].ID " +
            "FROM [GovCrossExchangeRateHistory] " +
            "LEFT JOIN [GovCrossExchangeRate] " +
            "ON [GovCrossExchangeRate].ID = [GovCrossExchangeRateHistory].GovCrossExchangeRateID " +
            "WHERE [GovCrossExchangeRate].NetUID = @NetId " +
            ") " +
            "SELECT * " +
            "FROM [GovCrossExchangeRateHistory] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [GovCrossExchangeRateHistory].UpdatedByID " +
            "WHERE [GovCrossExchangeRateHistory].ID IN ( " +
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

    public IEnumerable<GovCrossExchangeRate> GetAllByGovCrossExchangeRateNetIds(IEnumerable<Guid> netIds, long limit, long offset, DateTime from, DateTime to) {
        IEnumerable<GovCrossExchangeRate> crossExchangeRates =
            _connection.Query<GovCrossExchangeRate, Currency, Currency, GovCrossExchangeRate>(
                "SELECT * " +
                "FROM [GovCrossExchangeRate] " +
                "LEFT JOIN [views].[CurrencyView] AS [CurrencyFrom] " +
                "ON [CurrencyFrom].ID = [GovCrossExchangeRate].CurrencyFromID " +
                "AND [CurrencyFrom].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [CurrencyTo] " +
                "ON [CurrencyTo].ID = [GovCrossExchangeRate].CurrencyToID " +
                "AND [CurrencyTo].CultureCode = @Culture " +
                "WHERE [GovCrossExchangeRate].NetUID IN @NetIds ",
                (crossExchangeRate, currencyFrom, currencyTo) => {
                    crossExchangeRate.CurrencyFrom = currencyFrom;
                    crossExchangeRate.CurrencyTo = currencyTo;

                    return crossExchangeRate;
                },
                new { NetIds = netIds, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

        foreach (GovCrossExchangeRate crossExchangeRate in crossExchangeRates)
            crossExchangeRate.GovCrossExchangeRateHistories =
                _connection.Query<GovCrossExchangeRateHistory, User, GovCrossExchangeRateHistory>(
                    ";WITH [Search_CTE] " +
                    "AS " +
                    "( " +
                    "SELECT ROW_NUMBER() OVER (ORDER BY [GovCrossExchangeRateHistory].Created) AS RowNumber " +
                    ", [GovCrossExchangeRateHistory].ID " +
                    "FROM [GovCrossExchangeRateHistory] " +
                    "WHERE [GovCrossExchangeRateHistory].GovCrossExchangeRateID = @Id " +
                    "AND [GovCrossExchangeRateHistory].Created >= @From " +
                    "AND [GovCrossExchangeRateHistory].Created <= @To " +
                    ") " +
                    "SELECT * " +
                    "FROM [GovCrossExchangeRateHistory] " +
                    "LEFT JOIN [User] " +
                    "ON [User].ID = [GovCrossExchangeRateHistory].UpdatedByID " +
                    "WHERE [GovCrossExchangeRateHistory].ID IN ( " +
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

    public GovCrossExchangeRateHistory GetLatestByCrossExchangeRateNetId(Guid netId) {
        return _connection.Query<GovCrossExchangeRateHistory>(
            "SELECT TOP(1) [GovCrossExchangeRateHistory].* " +
            "FROM [GovCrossExchangeRateHistory] " +
            "LEFT JOIN [GovCrossExchangeRate] " +
            "ON [GovCrossExchangeRate].ID = [GovCrossExchangeRateHistory].GovCrossExchangeRateID " +
            "WHERE [GovCrossExchangeRate].NetUID = @NetId " +
            "ORDER BY [GovCrossExchangeRateHistory].Created",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public GovCrossExchangeRateHistory GetLatestNearToDateByCrossExchangeRateNetId(Guid netId, DateTime fromDate) {
        return _connection.Query<GovCrossExchangeRateHistory>(
            "SELECT TOP(1) [GovCrossExchangeRateHistory].* " +
            "FROM [GovCrossExchangeRateHistory] " +
            "LEFT JOIN [GovCrossExchangeRate] " +
            "ON [GovCrossExchangeRate].ID = [GovCrossExchangeRateHistory].GovCrossExchangeRateID " +
            "WHERE [GovCrossExchangeRate].NetUID = @NetId " +
            "AND [GovCrossExchangeRateHistory].Created <= @FromDate " +
            "ORDER BY [GovCrossExchangeRateHistory].Created DESC",
            new { NetId = netId, FromDate = fromDate }
        ).SingleOrDefault();
    }

    public GovCrossExchangeRateHistory GetFirstByGovCrossExchangeRateId(long id) {
        return _connection.Query<GovCrossExchangeRateHistory>(
            "SELECT TOP(1) [GovCrossExchangeRateHistory].* " +
            "FROM [GovCrossExchangeRateHistory] " +
            "WHERE [GovCrossExchangeRateHistory].GovCrossExchangeRateID = @Id " +
            "ORDER BY [GovCrossExchangeRateHistory].Created DESC ",
            new { Id = id }
        ).SingleOrDefault();
    }

    public GovCrossExchangeRateHistory GetById(long id) {
        return _connection.Query<GovCrossExchangeRateHistory>(
            "SELECT * " +
            "FROM [GovCrossExchangeRateHistory] " +
            "WHERE [GovCrossExchangeRateHistory].ID = @Id ",
            new { Id = id }
        ).FirstOrDefault();
    }
}