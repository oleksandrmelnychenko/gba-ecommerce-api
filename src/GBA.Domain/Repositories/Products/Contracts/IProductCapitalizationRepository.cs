using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductCapitalizationRepository {
    long Add(ProductCapitalization productCapitalization);

    ProductCapitalization GetLastRecord(string prefix);

    ProductCapitalization GetById(long id);

    ProductCapitalization GetByNetId(Guid netId);

    List<ProductCapitalization> GetAllFiltered(DateTime from, DateTime to, long limit, long offset);

    List<ProductCapitalization> GetAllFiltered(DateTime from, DateTime to);

    void UpdateRemainingQty(ProductCapitalizationItem item);
}