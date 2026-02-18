using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductCapitalizationItemRepository {
    void Add(IEnumerable<ProductCapitalizationItem> items);

    long Add(ProductCapitalizationItem item);

    void UpdateRemainingQty(ProductCapitalizationItem item);
}