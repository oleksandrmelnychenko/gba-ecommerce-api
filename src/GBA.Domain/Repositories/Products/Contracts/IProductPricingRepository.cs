using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductPricingRepository {
    void Add(ProductPricing productPricing);

    void Add(IEnumerable<ProductPricing> productPricings);

    void Update(ProductPricing productPricing);

    void Update(IEnumerable<ProductPricing> productPricings);

    void Remove(ProductPricing productPricing);

    void Remove(IEnumerable<ProductPricing> productPricings);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByProductId(long productId);

    ProductPricing GetByIdsIfExists(long productId, long pricingId);
}