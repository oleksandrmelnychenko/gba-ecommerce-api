using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductPlacementRepository {
    void Add(ProductPlacement productPlacement);

    long AddWithId(ProductPlacement productPlacement);

    void Add(IEnumerable<ProductPlacement> productPlacements);

    void UpdateQty(ProductPlacement productPlacement);

    void UpdateReferences(ProductPlacement productPlacement);

    void Update(ProductPlacement productPlacement);

    void Update(List<ProductPlacement> productPlacements);

    void Remove(ProductPlacement productPlacement);

    void RemoveWithoutQty(ProductPlacement productPlacement);

    void Restore(ProductPlacement productPlacement);

    void ReAssignProductPlacementFromProductIncomeItemToConsignmentItemByIds(long productIncomeItemId, long consignmentItemId);

    ProductPlacement GetById(long id);
    ProductPlacement GetByIdDeleted(long id);

    ProductPlacement GetIfExists(
        string rowNumber,
        string cellNumber,
        string storageNumber,
        long productId,
        long storageId,
        long? productIncomeId = null,
        long? consignmentItemId = null);

    ProductPlacement Get(
        string rowNumber,
        string cellNumber,
        string storageNumber,
        long productId,
        long storageId);

    ProductPlacement GetQty(
        int qty,
        long productId,
        long storageId);


    void RemoveFromProductIdToStorageId(
        long productId,
        long storageId);

    ProductPlacement GetByIdWithStorage(long id);

    ProductPlacement GetLastByProductAndStorageId(long productId, long storageId);

    ProductPlacement GetLastByProductId(long productId);
    ProductPlacement GetLastProductId(long productId);

    ProductPlacement GetNonByProductAndStorageId(long productId, long storageId);

    IEnumerable<ProductPlacement> GetAllByProductAndStorageId(long productId, long storageId);

    IEnumerable<ProductPlacement> GetAllByProductAndStorageIds(long productId, long storageId);

    IEnumerable<ProductPlacement> GetAllByProductIncomeItemId(long productIncomeItemId);
    IEnumerable<ProductPlacement> GetIsHistorySet(long productId, long storageId);
    void RemoveIsHistorySet(ProductPlacement productPlacement);

    IEnumerable<ProductPlacement> GetAllByConsignmentItemId(long consignmentItemId);
    IEnumerable<ProductPlacement> GetAllByConsignmentItemProductAndStorageIds(long consignmentItemId, long productId, long storageId);

    IEnumerable<ProductPlacement> GetAllFilteredAndOrderedByWriteOffRule(
        long storageId,
        long productId,
        string rowNumber,
        string cellNumber,
        string storageNumber,
        ProductWriteOffRuleType ruleType);
}