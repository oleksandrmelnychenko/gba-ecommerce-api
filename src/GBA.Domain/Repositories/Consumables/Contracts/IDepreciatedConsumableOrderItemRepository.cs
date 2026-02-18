using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface IDepreciatedConsumableOrderItemRepository {
    long Add(DepreciatedConsumableOrderItem depreciatedConsumableOrderItem);

    void Add(IEnumerable<DepreciatedConsumableOrderItem> depreciatedConsumableOrderItems);

    void Update(IEnumerable<DepreciatedConsumableOrderItem> depreciatedConsumableOrderItems);

    void UpdateAndRestore(IEnumerable<DepreciatedConsumableOrderItem> depreciatedConsumableOrderItems);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByOrderId(long id);

    void RemoveAllByOrderAndProductIds(long orderId, long productId);

    IEnumerable<DepreciatedConsumableOrderItem> GetAllByOrderId(long id);
}