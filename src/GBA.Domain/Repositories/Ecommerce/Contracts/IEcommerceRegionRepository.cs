using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Repositories.Ecommerce.Contracts;

public interface IEcommerceRegionRepository {
    IEnumerable<EcommerceRegion> GetAll();

    EcommerceRegion GetByNetId(Guid netId);

    long Add(EcommerceRegion ecommerceRegion);

    void Update(EcommerceRegion ecommerceRegion);
}
