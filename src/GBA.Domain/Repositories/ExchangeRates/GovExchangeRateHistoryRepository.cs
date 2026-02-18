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

public sealed class GovExchangeRateHistoryRepository : IGovExchangeRateHistoryRepository {
    private readonly IDbConnection _connection;

    public GovExchangeRateHistoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(GovExchangeRateHistory govExchangeRateHistory) {
        return _connection.Query<long>(
            "INSERT INTO [GovExchangeRateHistory] (Amount, GovExchangeRateId, UpdatedById, Updated) " +
            "VALUES (@Amount, @GovExchangeRateId, @UpdatedById, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            govExchangeRateHistory
        ).Single();
    }

    public long AddSpecific(GovExchangeRateHistory govExchangeRateHistory) {
        return _connection.Query<long>(
            "INSERT INTO [GovExchangeRateHistory] (Amount, GovExchangeRateId, UpdatedById, Created, Updated) " +
            "VALUES (@Amount, @GovExchangeRateId, @UpdatedById, @Created, @Updated); " +
            "SELECT SCOPE_IDENTITY()",
            govExchangeRateHistory
        ).Single();
    }

    public IEnumerable<GovExchangeRateHistory> GetAllByExchangeRateId(long id, long limit, long offset) {
        return _connection.Query<GovExchangeRateHistory, User, GovExchangeRateHistory>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [GovExchangeRateHistory].ID DESC) AS RowNumber " +
            ", [GovExchangeRateHistory].ID " +
            "FROM [GovExchangeRateHistory] " +
            "WHERE [GovExchangeRateHistory].GovExchangeRateID = @Id " +
            ") " +
            "SELECT * " +
            "FROM [GovExchangeRateHistory] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [GovExchangeRateHistory].UpdatedByID " +
            "WHERE [GovExchangeRateHistory].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ") ",
            (history, user) => {
                history.UpdatedBy = user;

                return history;
            },
            new { Id = id, Limit = limit, Offset = offset }
        );
    }

    public IEnumerable<GovExchangeRateHistory> GetAllByExchangeRateNetId(Guid netId, long limit, long offset) {
        return _connection.Query<GovExchangeRateHistory, User, GovExchangeRateHistory>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [GovExchangeRateHistory].Created DESC) AS RowNumber " +
            ", [GovExchangeRateHistory].ID " +
            "FROM [GovExchangeRateHistory] " +
            "LEFT JOIN [GovExchangeRate] " +
            "ON [GovExchangeRate].ID = [GovExchangeRateHistory].GovExchangeRateID " +
            "WHERE [GovExchangeRate].NetUID = @NetId " +
            ") " +
            "SELECT * " +
            "FROM [GovExchangeRateHistory] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [GovExchangeRateHistory].UpdatedByID " +
            "WHERE [GovExchangeRateHistory].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ") ",
            (history, user) => {
                history.UpdatedBy = user;

                return history;
            },
            new { NetId = netId, Limit = limit, Offset = offset }
        );
    }

    public IEnumerable<GovExchangeRate> GetAllByExchangeRateNetIds(IEnumerable<Guid> netIds, long limit, long offset, DateTime from, DateTime to) {
        IEnumerable<GovExchangeRate> exchangeRates =
            _connection.Query<GovExchangeRate, Currency, GovExchangeRate>(
                "SELECT * " +
                "FROM [GovExchangeRate] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [GovExchangeRate].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "WHERE [GovExchangeRate].NetUID IN @NetIds",
                (exchangeRate, currency) => {
                    exchangeRate.AssignedCurrency = currency;

                    return exchangeRate;
                },
                new { NetIds = netIds, Limit = limit, Offset = offset, From = from, To = to, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

        foreach (GovExchangeRate exchangeRate in exchangeRates)
            exchangeRate.GovExchangeRateHistories = _connection.Query<GovExchangeRateHistory, User, GovExchangeRateHistory>(
                ";WITH [Search_CTE] " +
                "AS " +
                "( " +
                "SELECT ROW_NUMBER() OVER (ORDER BY [GovExchangeRateHistory].Created) AS RowNumber " +
                ", [GovExchangeRateHistory].ID " +
                "FROM [GovExchangeRateHistory] " +
                "WHERE [GovExchangeRateHistory].GovExchangeRateID = @Id " +
                "AND [GovExchangeRateHistory].Created >= @From " +
                "AND [GovExchangeRateHistory].Created <= @To " +
                ") " +
                "SELECT * " +
                "FROM [GovExchangeRateHistory] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [GovExchangeRateHistory].UpdatedByID " +
                "WHERE [GovExchangeRateHistory].ID IN ( " +
                "SELECT [Search_CTE].ID " +
                "FROM [Search_CTE] " +
                "WHERE [Search_CTE].RowNumber > @Offset " +
                "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
                ")",
                (history, user) => {
                    history.UpdatedBy = user;

                    return history;
                },
                new { exchangeRate.Id, Limit = limit, Offset = offset, From = from, To = to }
            ).ToList();

        return exchangeRates;
    }

    public GovExchangeRateHistory GetLatestByExchangeRateNetId(Guid netId) {
        return _connection.Query<GovExchangeRateHistory>(
            "SELECT TOP(1) [GovExchangeRateHistory].* " +
            "FROM [GovExchangeRateHistory] " +
            "LEFT JOIN [GovExchangeRate] " +
            "ON [GovExchangeRate].ID = [GovExchangeRateHistory].GovExchangeRateID " +
            "WHERE [GovExchangeRate].NetUID = @NetId " +
            "ORDER BY [GovExchangeRateHistory].Created",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public GovExchangeRateHistory GetFirstByExchangeRateId(long id) {
        return _connection.Query<GovExchangeRateHistory>(
            "SELECT TOP(1) [GovExchangeRateHistory].* " +
            "FROM [GovExchangeRateHistory] " +
            "WHERE [GovExchangeRateHistory].GovExchangeRateID = @Id " +
            "ORDER BY [GovExchangeRateHistory].ID DESC ",
            new { Id = id }
        ).SingleOrDefault();
    }

    public GovExchangeRateHistory GetLatestNearToDateByExchangeRateNetId(Guid netId, DateTime fromDate) {
        return _connection.Query<GovExchangeRateHistory>(
            "SELECT TOP(1) [GovExchangeRateHistory].* " +
            "FROM [GovExchangeRateHistory] " +
            "LEFT JOIN [GovExchangeRate] " +
            "ON [GovExchangeRate].ID = [GovExchangeRateHistory].GovExchangeRateID " +
            "WHERE [GovExchangeRateHistory].Created <= @FromDate " +
            "AND [GovExchangeRate].NetUID = @NetId " +
            "ORDER BY [GovExchangeRateHistory].ID DESC",
            new { NetId = netId, FromDate = fromDate }
        ).SingleOrDefault();
    }

    public GovExchangeRateHistory GetById(long id) {
        return _connection.Query<GovExchangeRateHistory>(
            "SELECT * " +
            "FROM [GovExchangeRateHistory] " +
            "WHERE [GovExchangeRateHistory].ID = @Id ",
            new { Id = id }
        ).FirstOrDefault();
    }
}