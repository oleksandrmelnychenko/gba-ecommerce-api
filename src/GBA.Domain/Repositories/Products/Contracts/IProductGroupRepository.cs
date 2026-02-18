using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers.ProductGroupModels;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductGroupRepository {
    long Add(ProductGroup productGroup);

    void Update(ProductGroup productGroup);

    ProductGroup GetById(long id);

    ProductGroup GetByNetId(Guid netId);

    ProductGroup GetByName(string name);

    List<ProductGroup> GetAll();

    List<ProductGroup> GetAllByProductNetId(Guid productNetId);

    void Remove(Guid netId);

    ProductGroupsWithTotalModel GetAllFiltered(
        string value);

    ProductGroup GetByNetIdWithRootGroups(Guid netId);

    ProductSubGroupsWithTotalModel GetFilteredSubGroupsProductGroup(
        Guid netId,
        int limit,
        int offset,
        string value);

    List<ProductGroup> GetRootProductGroupsByNetId(Guid netId);

    ProductProductGroupsWithTotalModel GetFilteredProductByProductGroupNetId(
        Guid netId,
        int limit,
        int offset,
        string value);

    void SetIsSubGroup(long id);

    IEnumerable<ProductGroup> GetAllForReSaleAvailabilities();
}