using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductGroupDiscountRepository {
    long Add(ProductGroupDiscount productGroupDiscount);

    void Add(IEnumerable<ProductGroupDiscount> productGroupDiscounts);

    void Update(ProductGroupDiscount productGroupDiscount);

    void Update(IEnumerable<ProductGroupDiscount> productGroupDiscount);

    void Remove(IEnumerable<ProductGroupDiscount> productGroupDiscount);

    void RemoveAllByIds(IEnumerable<long> ids);

    List<ProductGroupDiscount> GetAllByClientAgreementIds(IEnumerable<long> ids);

    List<ProductGroupDiscount> GetAllByClientId(long id);

    ProductGroupDiscount GetById(long id);

    ProductGroupDiscount GetByProductGroupAndClientAgreementIdsIfExists(long clientAgreementId, long productGroupId);
}