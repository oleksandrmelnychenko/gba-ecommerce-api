using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductPlacementHistoryRepository {
    void Add(ProductPlacementHistory productPlacementHistory);

    long AddWithId(ProductPlacementHistory productPlacement);

    ProductPlacementHistory GetById(long id);

    ProductPlacementHistory GetLastByProductAndStorageId(long productId, long storageId);

    ProductPlacementHistory GetNonByProductAndStorageId(long productId, long storageId);

    IEnumerable<ProductPlacementHistory> GetAllByProductAndStorageId(long productId);

    IEnumerable<ProductPlacementHistory> GetAllByProductId(long productId, DateTime from, DateTime to, long limit, long offset);
}