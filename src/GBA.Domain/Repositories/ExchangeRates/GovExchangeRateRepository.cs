using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Repositories.ExchangeRates.Contracts;

namespace GBA.Domain.Repositories.ExchangeRates;

public sealed class GovExchangeRateRepository : IGovExchangeRateRepository {
    private readonly IDbConnection _connection;

    public GovExchangeRateRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(GovExchangeRate govExchangeRate) {
        return _connection.Query<long>(
            "INSERT INTO [GovExchangeRate] (Culture, Amount, Currency, Updated) " +
            "VALUES (@Culture, @Amount, @Currency, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            govExchangeRate
        ).Single();
    }

    public void Update(GovExchangeRate govExchangeRate) {
        _connection.Execute(
            "UPDATE [GovExchangeRate] SET " +
            "Culture = @Culture, Amount = @Amount, Currency = @Currency, Updated = getutcdate()" +
            "WHERE NetUID = @NetUid",
            govExchangeRate
        );
    }

    public GovExchangeRate GetById(long id) {
        return _connection.Query<GovExchangeRate>(
            "SELECT * " +
            "FROM [GovExchangeRate] " +
            "WHERE ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public GovExchangeRate GetByCurrencyIdAndCode(long id, string code) {
        return _connection.Query<GovExchangeRate>(
            "SELECT * " +
            "FROM [GovExchangeRate] " +
            "WHERE [GovExchangeRate].CurrencyID = @Id " +
            "AND [GovExchangeRate].Code = @Code ",
            new { Id = id, Code = code }
        ).FirstOrDefault();
    }

    public GovExchangeRate GetByCurrencyIdAndCode(long id, string code, DateTime fromDate) {
        return _connection.Query<GovExchangeRate>(
            "SELECT TOP(1) " +
            "[GovExchangeRate].ID, " +
            "(CASE " +
            "WHEN [GovExchangeRateHistory].Amount IS NOT NULL " +
            "THEN [GovExchangeRateHistory].Amount " +
            "ELSE [GovExchangeRate].Amount " +
            "END) AS [Amount] " +
            "FROM [GovExchangeRate] " +
            "LEFT JOIN [GovExchangeRateHistory] " +
            "ON [GovExchangeRateHistory].GovExchangeRateID = [GovExchangeRate].ID " +
            "AND [GovExchangeRateHistory].Created <= @FromDate " +
            "WHERE [GovExchangeRate].CurrencyID = @Id " +
            "AND [GovExchangeRate].Code = @Code " +
            "ORDER BY [GovExchangeRateHistory].ID DESC",
            new { Id = id, Code = code, FromDate = fromDate }
        ).FirstOrDefault();
    }

    public GovExchangeRate GetByNetId(Guid netId) {
        return _connection.Query<GovExchangeRate>(
            "SELECT * " +
            "FROM [GovExchangeRate] " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        ).SingleOrDefault();
    }

    public GovExchangeRate GetEuroGovExchangeRateByCurrentCulture() {
        return _connection.Query<GovExchangeRate>(
            "SELECT TOP(1) * " +
            "FROM [GovExchangeRate] " +
            "WHERE [GovExchangeRate].Deleted = 0 " +
            "AND [GovExchangeRate].Culture = @Culture " +
            "AND [GovExchangeRate].Code = 'EUR'",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).SingleOrDefault();
    }

    public List<GovExchangeRate> GetAllByCulture() {
        return _connection.Query<GovExchangeRate>(
            "SELECT * " +
            "FROM [GovExchangeRate] " +
            "WHERE [GovExchangeRate].Deleted = 0 " +
            "AND [GovExchangeRate].Culture = @Culture",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).ToList();
    }

    public List<GovExchangeRate> GetAll() {
        return _connection.Query<GovExchangeRate>(
            "SELECT * " +
            "FROM [GovExchangeRate] " +
            "WHERE [GovExchangeRate].Deleted = 0",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [GovExchangeRate] SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }

    public GovExchangeRate GetExchangeRateByCurrencyIdAndCode(long uahId, string code) {
        return _connection.Query<GovExchangeRate>(
            "SELECT TOP 1 * FROM [GovExchangeRate] " +
            "WHERE [GovExchangeRate].[CurrencyID] = @Id " +
            "AND [GovExchangeRate].[Code] = @Code " +
            "AND [GovExchangeRate].[Deleted] = 0; ",
            new { Id = uahId, Code = code }).FirstOrDefault();
    }
}