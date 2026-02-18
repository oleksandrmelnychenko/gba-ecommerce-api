using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductProductGroupRepository {
    void Add(ProductProductGroup productProductGroup);

    void Add(IEnumerable<ProductProductGroup> productProductGroups);

    void Update(ProductProductGroup productProductGroup);

    void Update(IEnumerable<ProductProductGroup> productProductGroups);

    void Remove(ProductProductGroup productProductGroup);

    void Remove(IEnumerable<ProductProductGroup> productProductGroups);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByProductId(long productId);

    List<ProductProductGroup> GetFilteredByProductGroupNetId(
        Guid netId,
        int limit,
        int offset,
        string value);
}