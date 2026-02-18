using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.Currencies.Contracts;

public interface ICurrencyTraderExchangeRateRepository {
    long Add(CurrencyTraderExchangeRate currencyTraderExchangeRate);

    void Add(IEnumerable<CurrencyTraderExchangeRate> currencyTraderExchangeRates);

    void Update(CurrencyTraderExchangeRate currencyTraderExchangeRate);

    void Update(IEnumerable<CurrencyTraderExchangeRate> currencyTraderExchangeRates);

    CurrencyTraderExchangeRate GetByNetId(Guid netId);

    IEnumerable<CurrencyTraderExchangeRate> GetAllByTraderIdFiltered(long id, DateTime from, DateTime to);

    void Remove(long id);

    void Remove(IEnumerable<long> ids);

    void RemoveByFromDateAndTraderId(DateTime fromDate, long id);
}