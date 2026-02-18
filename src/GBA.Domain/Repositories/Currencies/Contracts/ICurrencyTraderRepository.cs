using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.Currencies.Contracts;

public interface ICurrencyTraderRepository {
    long Add(CurrencyTrader currencyTrader);

    void Update(CurrencyTrader currencyTrader);

    CurrencyTrader GetById(long id);

    CurrencyTrader GetByNetId(Guid netId);

    IEnumerable<CurrencyTrader> GetAll();

    IEnumerable<CurrencyTrader> GetAllFromSearch(string value);

    IEnumerable<CurrencyTrader> FindByPaymentCurrencyRegisterNetId(string currencyCode);

    void Remove(Guid netId);
}