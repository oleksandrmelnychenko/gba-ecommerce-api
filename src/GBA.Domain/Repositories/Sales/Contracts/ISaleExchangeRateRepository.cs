using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface ISaleExchangeRateRepository {
    void Add(SaleExchangeRate saleExchangeRate);

    void Add(IEnumerable<SaleExchangeRate> saleExchangeRates);

    void Update(SaleExchangeRate saleExchangeRate);

    void Update(IEnumerable<SaleExchangeRate> saleExchangeRates);

    void Remove(SaleExchangeRate saleExchangeRate);

    void Remove(IEnumerable<SaleExchangeRate> saleExchangeRates);

    SaleExchangeRate GetEuroSaleExchangeRateBySaleNetId(Guid netId);

    List<SaleExchangeRate> GetAllBySaleNetId(Guid saleNetId);
}