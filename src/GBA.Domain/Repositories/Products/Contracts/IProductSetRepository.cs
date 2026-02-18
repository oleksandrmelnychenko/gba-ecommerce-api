using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductSetRepository {
    long Add(ProductSet productSet);

    void Add(IEnumerable<ProductSet> productSets);

    void Update(ProductSet productSet);

    void Update(IEnumerable<ProductSet> productSets);

    void Remove(ProductSet productSet);

    void Remove(IEnumerable<ProductSet> productSets);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByProductId(long productId);

    void DeleteByBaseProductNetIdAndComponentNetId(Guid baseProductNetId, Guid componentNetId);

    void DeleteAllByIds(IEnumerable<long> ids);

    bool CheckIsProductSetExistsByBaseProductAndComponentIds(long baseProductId, long componentId);

    bool CheckIfProductSetExistsByBaseProductAndComponentNetIds(Guid baseProductNetId, Guid componentNetId);

    ProductSet GetByProductAndComponentIds(long baseProductId, long componentId);
}