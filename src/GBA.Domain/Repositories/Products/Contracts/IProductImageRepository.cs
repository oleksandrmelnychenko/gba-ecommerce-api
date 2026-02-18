using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductImageRepository {
    void Add(IEnumerable<ProductImage> images);

    void Update(IEnumerable<ProductImage> images);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByProductId(long productId);
}