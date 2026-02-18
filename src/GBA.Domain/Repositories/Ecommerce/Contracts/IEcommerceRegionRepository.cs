using System.Collections.Generic;
using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Repositories.Ecommerce.Contracts;

public interface IEcommerceRegionRepository {
    IEnumerable<EcommerceRegion> GetAll();

    long Add(EcommerceRegion ecommerceRegion);

    void Update(EcommerceRegion ecommerceRegion);
}