using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductCategoryRepository {
    void Add(ProductCategory productCategory);

    void Add(IEnumerable<ProductCategory> productCategories);

    void Update(ProductCategory productCategory);

    void Update(IEnumerable<ProductCategory> productCategories);

    void Remove(ProductCategory productCategory);

    void Remove(IEnumerable<ProductCategory> productCategories);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByProductId(long productId);
}