using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductSubGroupRepository {
    void Add(ProductSubGroup productSubGroup);

    void Add(IEnumerable<ProductSubGroup> productSubGroups);

    void Update(ProductSubGroup productSubGroup);

    void Update(IEnumerable<ProductSubGroup> productSubGroups);

    void Remove(ProductSubGroup productSubGroup);

    void Remove(IEnumerable<ProductSubGroup> productSubGroups);

    void RemoveAllByIds(IEnumerable<long> ids);

    ProductSubGroup GetByRootAndSubIds(long rootId, long subId);

    void Restore(long id);
}