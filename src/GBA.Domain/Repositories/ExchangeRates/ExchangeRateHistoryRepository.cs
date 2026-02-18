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

public sealed class ExchangeRateHistoryRepository : IExchangeRateHistoryRepository {
    private readonly IDbConnection _connection;

    public ExchangeRateHistoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ExchangeRateHistory exchangeRateHistory) {
        return _connection.Query<long>(
            "INSERT INTO [ExchangeRateHistory] (Amount, ExchangeRateId, UpdatedById, Updated) " +
            "VALUES (@Amount, @ExchangeRateId, @UpdatedById, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            exchangeRateHistory
        ).Single();
    }

    public long AddSpecific(ExchangeRateHistory exchangeRateHistory) {
        return _connection.Query<long>(
            "INSERT INTO [ExchangeRateHistory] (Amount, ExchangeRateId, UpdatedById, Created, Updated) " +
            "VALUES (@Amount, @ExchangeRateId, @UpdatedById, @Created, @Updated); " +
            "SELECT SCOPE_IDENTITY()",
            exchangeRateHistory
        ).Single();
    }

    public IEnumerable<ExchangeRateHistory> GetAllByExchangeRateId(long id, long limit, long offset) {
        return _connection.Query<ExchangeRateHistory, User, ExchangeRateHistory>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [ExchangeRateHistory].Created DESC) AS RowNumber " +
            ", [ExchangeRateHistory].ID " +
            "FROM [ExchangeRateHistory] " +
            "WHERE [ExchangeRateHistory].ExchangeRateID = @Id " +
            ") " +
            "SELECT * " +
            "FROM [ExchangeRateHistory] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ExchangeRateHistory].UpdatedByID " +
            "WHERE [ExchangeRateHistory].ID IN ( " +
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

    public IEnumerable<ExchangeRateHistory> GetAllByExchangeRateNetId(Guid netId, long limit, long offset) {
        return _connection.Query<ExchangeRateHistory, User, ExchangeRateHistory>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [ExchangeRateHistory].Created DESC) AS RowNumber " +
            ", [ExchangeRateHistory].ID " +
            "FROM [ExchangeRateHistory] " +
            "LEFT JOIN [ExchangeRate] " +
            "ON [ExchangeRate].ID = [ExchangeRateHistory].ExchangeRateID " +
            "WHERE [ExchangeRate].NetUID = @NetId " +
            ") " +
            "SELECT * " +
            "FROM [ExchangeRateHistory] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ExchangeRateHistory].UpdatedByID " +
            "WHERE [ExchangeRateHistory].ID IN ( " +
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

    public IEnumerable<ExchangeRate> GetAllByExchangeRateNetIds(IEnumerable<Guid> netIds, long limit, long offset, DateTime from, DateTime to) {
        IEnumerable<ExchangeRate> exchangeRates = _connection.Query<ExchangeRate, Currency, ExchangeRate>(
            "SELECT * " +
            "FROM [ExchangeRate] " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [ExchangeRate].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [ExchangeRate].NetUID IN @NetIds",
            (exchangeRate, currency) => {
                exchangeRate.AssignedCurrency = currency;

                return exchangeRate;
            },
            new { NetIds = netIds, Limit = limit, Offset = offset, From = from, To = to, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        foreach (ExchangeRate exchangeRate in exchangeRates)
            exchangeRate.ExchangeRateHistories = _connection.Query<ExchangeRateHistory, User, ExchangeRateHistory>(
                ";WITH [Search_CTE] " +
                "AS " +
                "( " +
                "SELECT ROW_NUMBER() OVER (ORDER BY [ExchangeRateHistory].Created) AS RowNumber " +
                ", [ExchangeRateHistory].ID " +
                "FROM [ExchangeRateHistory] " +
                "WHERE [ExchangeRateHistory].ExchangeRateID = @Id " +
                "AND [ExchangeRateHistory].Created >= @From " +
                "AND [ExchangeRateHistory].Created <= @To " +
                ") " +
                "SELECT * " +
                "FROM [ExchangeRateHistory] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ExchangeRateHistory].UpdatedByID " +
                "WHERE [ExchangeRateHistory].ID IN ( " +
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

    public ExchangeRateHistory GetLatestByExchangeRateNetId(Guid netId) {
        return _connection.Query<ExchangeRateHistory>(
            "SELECT TOP(1) [ExchangeRateHistory].* " +
            "FROM [ExchangeRateHistory] " +
            "LEFT JOIN [ExchangeRate] " +
            "ON [ExchangeRate].ID = [ExchangeRateHistory].ExchangeRateID " +
            "WHERE [ExchangeRate].NetUID = @NetId " +
            "ORDER BY [ExchangeRateHistory].Created",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public ExchangeRateHistory GetLatestNearToDateByExchangeRateNetId(Guid netId, DateTime fromDate) {
        return _connection.Query<ExchangeRateHistory>(
            "SELECT TOP(1) [ExchangeRateHistory].* " +
            "FROM [ExchangeRateHistory] " +
            "LEFT JOIN [ExchangeRate] " +
            "ON [ExchangeRate].ID = [ExchangeRateHistory].ExchangeRateID " +
            "WHERE [ExchangeRateHistory].Created <= @FromDate " +
            "AND [ExchangeRate].NetUID = @NetId " +
            "ORDER BY [ExchangeRateHistory].Created DESC",
            new { NetId = netId, FromDate = fromDate }
        ).SingleOrDefault();
    }

    public ExchangeRateHistory GetFirstByExchangeRateId(long id) {
        return _connection.Query<ExchangeRateHistory>(
            "SELECT TOP(1) [ExchangeRateHistory].* " +
            "FROM [ExchangeRateHistory] " +
            "WHERE [ExchangeRateHistory].ExchangeRateID = @Id " +
            "ORDER BY [ExchangeRateHistory].Created DESC ",
            new { Id = id }
        ).SingleOrDefault();
    }

    public ExchangeRateHistory GetById(long id) {
        return _connection.Query<ExchangeRateHistory>(
            "SELECT * " +
            "FROM [ExchangeRateHistory] " +
            "WHERE [ExchangeRateHistory].ID = @Id ",
            new { Id = id }
        ).FirstOrDefault();
    }
}