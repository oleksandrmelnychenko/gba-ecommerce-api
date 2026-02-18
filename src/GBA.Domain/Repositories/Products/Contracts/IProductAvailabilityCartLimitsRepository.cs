using System.Collections.Generic;
using GBA.Common.Helpers.SupplyOrders;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductAvailabilityCartLimitsRepository {
    IEnumerable<CartItemRecommendedProduct> GetAll(long storageIdPl, long storageIdUk);
}