using System;
using System.Collections.Generic;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Repositories.ExchangeRates.Contracts;

public interface ICrossExchangeRateRepository {
    CrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId);

    CrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId, DateTime fromDate);

    List<CrossExchangeRate> GetAll();

    CrossExchangeRate GetByNetId(Guid netId);

    void Update(CrossExchangeRate crossExchangeRate);
}