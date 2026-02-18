using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Repositories.ExchangeRates.Contracts;

public interface IExchangeRateRepository {
    long Add(ExchangeRate exchangeRate);

    void Update(ExchangeRate exchangeRate);

    void Remove(Guid netId);

    decimal GetExchangeRateToEuroCurrency(Currency fromCurrency, bool fromPln = false);

    decimal GetEuroToUsdExchangeRateAmountByFromDate(DateTime fromDate);

    ExchangeRate GetById(long id);

    ExchangeRate GetByNetId(Guid netId);

    ExchangeRate GetByCurrencyIdAndCode(long id, string code);

    ExchangeRate GetByCurrencyIdAndCode(long id, string code, DateTime fromDate);

    ExchangeRate GetEuroExchangeRateByCurrentCulture();

    ExchangeRate GetByCurrencyCodeAndCurrentCulture(string code);

    List<ExchangeRate> GetAllByCulture();

    List<ExchangeRate> GetAll();

    decimal GetEuroExchangeRateByCurrentCultureFiltered(
        Guid productNetId,
        bool forVatProducts,
        bool isFromReSale,
        long currencyId);
}