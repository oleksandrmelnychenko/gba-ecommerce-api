using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISupplyOrderUkraineItemRepository {
    SupplyOrderUkraineItem GetById(long id);

    SupplyOrderUkraineItem GetNotOrderedItemByActReconciliationItemIdIfExists(long id);

    SupplyOrderUkraineItem GetByRefIdsIfExists(long productId, long orderUkraineId, long supplierId);

    SupplyOrderUkraineItem GetNotOrderedByRefIdsIfExists(long productId, long orderUkraineId, long supplierId);

    long Add(SupplyOrderUkraineItem item);

    void Add(IEnumerable<SupplyOrderUkraineItem> items);

    void Update(SupplyOrderUkraineItem item);

    void Update(IEnumerable<SupplyOrderUkraineItem> items);

    void UpdateWeightAndPrice(IEnumerable<SupplyOrderUkraineItem> items);

    void UpdatePlacementInformation(SupplyOrderUkraineItem item);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByOrderUkraineIdExceptProvided(long orderUkraineId, IEnumerable<long> ids);

    void IncreasePlacementInfoById(long id, double qty);

    decimal GetTotalEuroAmountForPlacedItemsByStorage(Guid storageNetId);

    decimal GetTotalEuroAmountForPlacedItemsFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        string value,
        DateTime from,
        DateTime to
    );

    IEnumerable<SupplyOrderUkraineItem> GetAllPlacedItemsFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        long limit,
        long offset,
        string value,
        DateTime from,
        DateTime to
    );

    IEnumerable<SupplyOrderUkraineItem> GetRemainingInfoByProductId(long productId);

    void UpdateGrossPrice(IEnumerable<SupplyOrderUkraineItem> items);

    Currency GetCurrencyFromOrderByItemId(long id);

    SupplyOrderUkraine GetOrderByItemId(long id);

    void UpdateRemainingQty(SupplyOrderUkraineItem item);
}