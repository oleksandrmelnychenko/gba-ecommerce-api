using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.Currencies.Contracts;

public interface ICurrencyRepository {
    long Add(Currency currency);

    void Update(Currency currency);

    Currency GetById(long id);

    Currency GetByNetId(Guid netId);

    Currency GetBase();

    Currency GetEURCurrencyIfExists();

    Currency GetUAHCurrencyIfExists();

    Currency GetPLNCurrencyIfExists();

    List<Currency> GetAll();

    bool IsCurrencyAttachedToAnyPricing(long currencyId);

    bool IsCurrencyAttachedToAnyAgreement(long currencyId);

    void Remove(Guid netId);

    Currency GetByContainerServiceId(long id);

    Currency GetByVehicleServiceId(long id);

    Currency GetByMergedServiceId(long id);

    Currency GetByBillOfLadingServiceId(long id);

    Currency GetUSDCurrencyIfExists();

    Currency GetByOneCCode(string code);
}