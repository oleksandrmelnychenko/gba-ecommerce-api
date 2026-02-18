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

public sealed class ExchangeRateRepository : IExchangeRateRepository {
    private readonly IDbConnection _connection;

    public ExchangeRateRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ExchangeRate exchangeRate) {
        return _connection.Query<long>(
            "INSERT INTO ExchangeRate (Culture, Amount, Currency, Updated, CurrencyID) " +
            "VALUES (@Culture, @Amount, @Currency, getutcdate(), @CurrencyId); " +
            "SELECT SCOPE_IDENTITY()",
            exchangeRate
        ).Single();
    }

    public void Update(ExchangeRate exchangeRate) {
        _connection.Execute(
            "UPDATE ExchangeRate SET " +
            "Culture = @Culture, Amount = @Amount, Currency = @Currency, Updated = getutcdate(), CurrencyID = @CurrencyId " +
            "WHERE NetUID = @NetUid",
            exchangeRate
        );
    }

    public ExchangeRate GetById(long id) {
        return _connection.Query<ExchangeRate>(
            "SELECT * FROM ExchangeRate " +
            "WHERE ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public ExchangeRate GetByCurrencyIdAndCode(long id, string code) {
        return _connection.Query<ExchangeRate>(
            "SELECT * " +
            "FROM [ExchangeRate] " +
            "WHERE [ExchangeRate].CurrencyID = @Id " +
            "AND [ExchangeRate].Code = @Code ",
            new { Id = id, Code = code }
        ).FirstOrDefault();
    }

    public ExchangeRate GetByCurrencyIdAndCode(long id, string code, DateTime fromDate) {
        return _connection.Query<ExchangeRate>(
            "SELECT TOP(1) " +
            "[ExchangeRate].ID, " +
            "(CASE " +
            "WHEN [ExchangeRateHistory].Amount IS NOT NULL " +
            "THEN [ExchangeRateHistory].Amount " +
            "ELSE [ExchangeRate].Amount " +
            "END) AS [Amount] " +
            "FROM [ExchangeRate] " +
            "LEFT JOIN [ExchangeRateHistory] " +
            "ON [ExchangeRateHistory].ExchangeRateID = [ExchangeRate].ID " +
            "AND [ExchangeRateHistory].Created <= @FromDate " +
            "WHERE [ExchangeRate].CurrencyID = @Id " +
            "AND [ExchangeRate].Code = @Code " +
            "ORDER BY [ExchangeRateHistory].Created DESC",
            new { Id = id, Code = code, FromDate = fromDate }
        ).FirstOrDefault();
    }

    public ExchangeRate GetByNetId(Guid netId) {
        return _connection.Query<ExchangeRate>(
            "SELECT * FROM ExchangeRate " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        ).SingleOrDefault();
    }

    public ExchangeRate GetEuroExchangeRateByCurrentCulture() {
        return _connection.Query<ExchangeRate>(
            "SELECT TOP(1) * FROM ExchangeRate " +
            "WHERE ExchangeRate.Deleted = 0 " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR'",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).SingleOrDefault();
    }

    public List<ExchangeRate> GetAllByCulture() {
        return _connection.Query<ExchangeRate>(
            "SELECT * FROM ExchangeRate " +
            "WHERE ExchangeRate.Deleted = 0 " +
            "AND ExchangeRate.Culture = @Culture",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).ToList();
    }

    public List<ExchangeRate> GetAll() {
        return _connection.Query<ExchangeRate>(
            "SELECT * FROM ExchangeRate " +
            "WHERE ExchangeRate.Deleted = 0",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ExchangeRate SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }

    public decimal GetExchangeRateToEuroCurrency(Currency fromCurrency, bool fromPln = false) {
        decimal exchangeRateAmount;

        if (fromPln)
            fromCurrency = _connection.Query<Currency>(
                "SELECT TOP(1) * " +
                "FROM [Currency] " +
                "WHERE [Currency].Deleted = 0 " +
                "AND [Currency].Code = 'PLN'"
            ).Single();

        if (fromCurrency.Code.ToLower().Equals("uah") || fromCurrency.Code.ToLower().Equals("pln"))
            exchangeRateAmount = _connection.Query<decimal>(
                "DECLARE @ExchangeRate money; " +
                "SELECT @ExchangeRate = " +
                "( " +
                "SELECT [ExchangeRate].Amount " +
                "FROM [ExchangeRate] " +
                "WHERE [ExchangeRate].CurrencyID = @FromCurrencyId " +
                "AND [ExchangeRate].Code = 'EUR' " +
                "AND [ExchangeRate].Deleted = 0 " +
                "); " +
                "SELECT " +
                "CASE " +
                "WHEN @ExchangeRate IS NOT NULL " +
                "THEN @ExchangeRate " +
                "ELSE 1 " +
                "END",
                new { FromCurrencyId = fromCurrency.Id }
            ).Single();
        else
            exchangeRateAmount = _connection.Query<decimal>(
                "DECLARE @EuroCurrencyId bigint; " +
                "DECLARE @CrossExchangeRate money; " +
                "DECLARE @InverseCrossExchangeRate money; " +
                "SELECT @EuroCurrencyId = (SELECT TOP(1) [Currency].ID FROM [Currency] WHERE [Currency].Deleted = 0 AND [Currency].Code = 'EUR'); " +
                "SELECT @CrossExchangeRate = " +
                "( " +
                "SELECT [CrossExchangeRate].Amount " +
                "FROM [CrossExchangeRate] " +
                "WHERE [CrossExchangeRate].CurrencyFromID = @FromCurrencyId " +
                "AND [CrossExchangeRate].CurrencyToID = @EuroCurrencyId " +
                "AND [CrossExchangeRate].Deleted = 0 " +
                "); " +
                "SELECT @InverseCrossExchangeRate = " +
                "( " +
                "SELECT [CrossExchangeRate].Amount " +
                "FROM [CrossExchangeRate] " +
                "WHERE [CrossExchangeRate].CurrencyFromID = @EuroCurrencyId " +
                "AND [CrossExchangeRate].CurrencyToID = @FromCurrencyId " +
                "AND [CrossExchangeRate].Deleted = 0 " +
                "); " +
                "SELECT " +
                "CASE " +
                "WHEN @CrossExchangeRate IS NOT NULL " +
                "THEN @CrossExchangeRate " +
                "WHEN @InverseCrossExchangeRate IS NOT NULL " +
                "THEN @InverseCrossExchangeRate " +
                "ELSE 1 " +
                "END",
                new { FromCurrencyId = fromCurrency.Id }
            ).Single();

        return exchangeRateAmount;
    }

    public decimal GetEuroToUsdExchangeRateAmountByFromDate(DateTime fromDate) {
        return _connection.Query<decimal>(
            "DECLARE @EuroCurrencyId bigint = ( " +
            "SELECT TOP(1) ID " +
            "FROM [Currency] " +
            "WHERE [Currency].Deleted = 0 " +
            "AND [Currency].Code = N'eur' " +
            "); " +
            "DECLARE @UsdCurrencyId bigint = ( " +
            "SELECT TOP(1) ID " +
            "FROM [Currency] " +
            "WHERE [Currency].Deleted = 0 " +
            "AND [Currency].Code = N'usd' " +
            "); " +
            "SELECT TOP(1) " +
            "CASE " +
            "WHEN [CrossExchangeRateHistory].ID IS NOT NULL " +
            "THEN [CrossExchangeRateHistory].Amount " +
            "ELSE [CrossExchangeRate].Amount " +
            "END " +
            "FROM [CrossExchangeRate] " +
            "LEFT JOIN [CrossExchangeRateHistory] " +
            "ON [CrossExchangeRateHistory].CrossExchangeRateID = [CrossExchangeRate].ID " +
            "AND [CrossExchangeRateHistory].Deleted = 0 " +
            "AND [CrossExchangeRateHistory].Created <= @FromDate " +
            "WHERE [CrossExchangeRate].Deleted = 0 " +
            "AND [CrossExchangeRate].CurrencyFromID = @EuroCurrencyId " +
            "AND [CrossExchangeRate].CurrencyToID = @UsdCurrencyId " +
            "ORDER BY [CrossExchangeRateHistory].Created DESC",
            new { FromDate = fromDate }
        ).Single();
    }

    public decimal GetEuroExchangeRateByCurrentCultureFiltered(
        Guid productNetId,
        bool forVatProducts,
        bool isFromReSale,
        long currencyId) {
        return _connection.Query<decimal>(
            "SELECT " +
            "ISNULL ( " +
            "dbo.[GetCurrentEuroExchangeRateFiltered](@ProductNetId, @CurrencyId, @ForVatProducts, @IsFromReSale), " +
            "0.00) ",
            new {
                ProductNetId = productNetId,
                ForVatProducts = forVatProducts,
                IsFromReSale = isFromReSale,
                CurrencyId = currencyId
            }
        ).SingleOrDefault();
    }

    public ExchangeRate GetByCurrencyCodeAndCurrentCulture(string code) {
        return _connection.Query<ExchangeRate>(
            "SELECT * FROM ExchangeRate " +
            "WHERE ExchangeRate.Deleted = 0 " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = @Code ",
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Code = code
            }
        ).SingleOrDefault();
    }
}