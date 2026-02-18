using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products.Incomes;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductIncomeRepository {
    long Add(ProductIncome productIncome);

    void RemoveAllBySaleReturnItemIds(IEnumerable<long> ids);

    ProductIncome GetLastByCulture(string culture);

    ProductIncome GetById(long id);

    ProductIncome GetByNetId(Guid netId);

    ProductIncome GetBySupplyOrderNetId(Guid netId);

    ProductIncome GetLastByTypeAndPrefix(ProductIncomeType incomeType, string prefix);

    List<ProductIncome> GetAllBySupplyOrderUkraineNetId(Guid netId);

    List<ProductIncome> GetAll();

    List<ProductIncome> GetAllFiltered(DateTime from, DateTime to, long limit, long offset, string value);

    List<ProductIncome> GetAllFiltered(DateTime from, DateTime to);

    List<ProductIncome> GetAllByProductNetId(Guid netId);

    ProductIncome GetByNetIdForPrintingDocument(Guid netId);

    ProductIncome GetByIdForConsignmentCreate(long id);

    ProductIncome GetByDeliveryProductProtocolNetId(Guid netId);

    ProductIncome GetSupplyOrderUkraineProductIncomeByNetId(Guid netId);

    ProductIncome GetSupplyOrderProductIncomeByNetId(Guid netId);
}