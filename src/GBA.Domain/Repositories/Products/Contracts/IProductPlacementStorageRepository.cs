using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductPlacementStorageRepository {
    long Add(ProductPlacementStorage productPlacementStorage);

    ProductPlacementStorage GetById(long id);

    IEnumerable<ProductPlacementStorage> GetAllFiltered(
        long[] storageId,
        string value,
        DateTime to,
        long limit,
        long offset
    );

    IEnumerable<ProductPlacementStorage> GetAll();
}