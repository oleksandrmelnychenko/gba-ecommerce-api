using System;
using System.Collections.Generic;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Repositories.ExchangeRates.Contracts;

public interface IGovExchangeRateRepository {
    long Add(GovExchangeRate govExchangeRate);

    void Update(GovExchangeRate govExchangeRate);

    GovExchangeRate GetById(long id);

    GovExchangeRate GetByCurrencyIdAndCode(long id, string code);

    GovExchangeRate GetByCurrencyIdAndCode(long id, string code, DateTime fromDate);

    GovExchangeRate GetByNetId(Guid netId);

    GovExchangeRate GetEuroGovExchangeRateByCurrentCulture();

    List<GovExchangeRate> GetAllByCulture();

    List<GovExchangeRate> GetAll();

    void Remove(Guid netId);

    GovExchangeRate GetExchangeRateByCurrencyIdAndCode(long uahId, string code);
}