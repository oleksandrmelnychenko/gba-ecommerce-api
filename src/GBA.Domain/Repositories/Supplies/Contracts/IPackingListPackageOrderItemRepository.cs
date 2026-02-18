using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IPackingListPackageOrderItemRepository {
    long Add(PackingListPackageOrderItem packingListPackageOrderItem);

    void Add(IEnumerable<PackingListPackageOrderItem> packingListPackageOrderItems);

    void Update(PackingListPackageOrderItem packingListPackageOrderItem);

    void Update(IEnumerable<PackingListPackageOrderItem> packingListPackageOrderItems);

    void RemoveById(long id);

    void RemoveAllByPackageId(long packageId);

    void RemoveAllByPackageIdExceptProvided(long packageId, IEnumerable<long> ids);

    void RemoveAllByPackingListId(long packingListId);

    void RemoveAllByPackingListIdExceptProvided(long packingListId, IEnumerable<long> ids);

    void SetIsReadyToPlacedByNetId(Guid netId, bool value);

    void SetIsReadyToPlacedByPackingListNetId(Guid netId);

    void SetIsPlacedByIds(IEnumerable<long> ids, bool value);

    void SetIsPlacedOnlyByIds(IEnumerable<long> ids, bool value);

    void UpdateRemainingQty(PackingListPackageOrderItem packingListPackageOrderItem);

    void UpdatePlacementInformation(PackingListPackageOrderItem item);

    void UpdatePlacementInformation(long id, double qty);

    void UpdateRemainingQty(long id, double toAddQty);

    void UpdateVatPercent(IEnumerable<PackingListPackageOrderItem> packingListPackageOrderItems);

    void UpdatePackingListId(IEnumerable<long> ids, long packingListId);

    IEnumerable<PackingListPackageOrderItem> GetAllNotPlacedBySupplyInvoiceOrderItemId(long supplyInvoiceOrderItemId);

    IEnumerable<PackingListPackageOrderItem> GetAllArrivedItemsByProductIdOrderedByWriteOffRuleType(
        long productId,
        long storageId,
        ProductWriteOffRuleType writeOffRuleType,
        string supplierName = ""
    );

    IEnumerable<PackingListPackageOrderItem> GetAllArrivedItemsByProductIdWithSupplierOrderedByWriteOffRuleType(
        long productId,
        long storageId,
        ProductWriteOffRuleType writeOffRuleType,
        string supplierName = ""
    );

    decimal GetTotalEuroAmountForPlacedItemsByStorage(Guid storageNetId);

    decimal GetTotalEuroAmountForPlacedItemsFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        string value,
        DateTime from,
        DateTime to
    );

    List<PackingListPackageOrderItem> GetAllPlacedItemsFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        long limit,
        long offset,
        string value,
        DateTime from,
        DateTime to
    );

    List<PackingListPackageOrderItem> GetRemainingInfoByProductId(long productId);

    PackingListPackageOrderItem GetByIdForPlacement(long id);

    PackingListPackageOrderItem GetById(long id);

    PackingListPackageOrderItem GetByIdWithIncludesForProduct(long id);

    PackingListPackageOrderItem GetByNetId(Guid netId);
}