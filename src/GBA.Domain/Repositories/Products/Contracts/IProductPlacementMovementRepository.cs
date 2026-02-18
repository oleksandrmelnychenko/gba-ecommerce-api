using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductPlacementMovementRepository {
    long Add(ProductPlacementMovement productPlacementMovement);

    ProductPlacementMovement GetLastRecord(string locale);

    ProductPlacementMovement GetById(long id);

    IEnumerable<ProductPlacementMovement> GetAllFiltered(
        Guid storageNetId,
        string value,
        DateTime from,
        DateTime to,
        long limit,
        long offset
    );
}