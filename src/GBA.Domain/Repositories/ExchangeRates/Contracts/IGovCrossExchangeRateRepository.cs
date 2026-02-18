using System;
using System.Collections.Generic;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Repositories.ExchangeRates.Contracts;

public interface IGovCrossExchangeRateRepository {
    List<GovCrossExchangeRate> GetAll();

    GovCrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId);

    GovCrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId, DateTime fromDate);

    GovCrossExchangeRate GetByNetId(Guid netId);

    void Update(GovCrossExchangeRate govCrossExchangeRate);
}