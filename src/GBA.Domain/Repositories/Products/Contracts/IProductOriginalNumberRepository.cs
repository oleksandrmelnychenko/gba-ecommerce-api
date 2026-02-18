using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductOriginalNumberRepository {
    long Add(ProductOriginalNumber productOriginalNumber);

    void Add(IEnumerable<ProductOriginalNumber> productOriginalNumbers);

    void Update(ProductOriginalNumber productOriginalNumber);

    void Update(IEnumerable<ProductOriginalNumber> productOriginalNumbers);

    void SetNotMainByProductId(long id);

    void Remove(ProductOriginalNumber productOriginalNumbers);

    void Remove(IEnumerable<ProductOriginalNumber> productOriginalNumbers);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByProductId(long productId);

    ProductOriginalNumber GetMainByProductId(long productId);

    IEnumerable<ProductOriginalNumber> GetByProductId(long productId);

    ProductOriginalNumber GetByProductAndNumberId(long productId, long originalNumberId);

    void RemoveByOriginalNumberId(long originalNumberId);

    void DeleteAllByIds(IEnumerable<long> ids);

    bool CheckIsProductOriginalNumberExistsByBaseProductIdAndOriginalNumber(long baseProductId, string number);
}