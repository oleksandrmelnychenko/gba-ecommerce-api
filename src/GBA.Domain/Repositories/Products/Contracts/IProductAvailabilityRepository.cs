using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductAvailabilityRepository {
    void Add(ProductAvailability productAvailability);

    long AddWithId(ProductAvailability productAvailability);

    void Update(ProductAvailability productAvailability);

    void Update(List<ProductAvailability> productAvailabilities);

    void RemoveById(long id);

    ProductAvailability GetByProductIdForCulture(long id, string culture);

    ProductAvailability GetByProductAndStorageIds(long productId, long storageId);

    IEnumerable<ProductAvailability> GetByProductAndCultureIds(long productId, string culture);

    IEnumerable<ProductAvailability> GetAllByProductAndStorageIds(long productId, List<long> storageIds);

    IEnumerable<ProductAvailability> GetByProductAndOrganizationIds(long productId, long organizationId, bool vatStorage, bool withReSale = false, long? storageId = null);

    List<ProductAvailability> GetAllByStorageNetIdFiltered(Guid netId, long limit, long offset, string value);

    List<ProductAvailability> GetAllOnDefectiveStoragesByProductId(long id);

    List<ProductAvailability> GetAllProductsByStorageNetId(Guid netId);
}