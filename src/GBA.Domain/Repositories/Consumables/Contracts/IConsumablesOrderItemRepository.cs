using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface IConsumablesOrderItemRepository {
    long Add(ConsumablesOrderItem consumablesOrderItem);

    void Add(IEnumerable<ConsumablesOrderItem> consumablesOrderItems);

    void Update(IEnumerable<ConsumablesOrderItem> consumablesOrderItems);

    void Remove(IEnumerable<long> ids);

    void RemoveAllExceptProvided(long orderId, IEnumerable<long> ids);

    ConsumablesOrderItem GetByIdWithCalculatedUnDepreciatedQty(long id, long storageId);

    IEnumerable<ConsumablesOrderItem> GetAllUnDepreciatedByProductAndStorageIds(long productId, long storageId);

    IEnumerable<ConsumablesOrderItem> GetAllUnDepreciatedByProductAndStorageIdsMostExpensiveFirst(long productId, long storageId);

    IEnumerable<ConsumablesOrderItem> GetAllUnDepreciatedByProductAndStorageIdsExceptProvidedItemId(long productId, long storageId, long itemId);

    IEnumerable<ConsumablesOrderItem> GetAllUnDepreciatedByProductAndStorageIdsExceptProvidedItemIdMostExpensiveFirst(long productId, long storageId, long itemId);
}