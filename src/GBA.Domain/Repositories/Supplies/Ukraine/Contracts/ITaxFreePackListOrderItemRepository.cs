using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ITaxFreePackListOrderItemRepository {
    long Add(TaxFreePackListOrderItem taxFreePackListOrderItem);

    void Update(TaxFreePackListOrderItem taxFreePackListOrderItem);

    void Update(IEnumerable<TaxFreePackListOrderItem> items);

    void RestoreUnpackedQtyByTaxFreeItemsIds(IEnumerable<long> ids);

    void RestoreUnpackedQtyByTaxFreeIdExceptProvidedIds(long taxFreeId, IEnumerable<long> ids);

    void DecreaseUnpackedQtyById(long id, double qty);

    void SetUnpackedQtyToAllItemsByOrderId(long orderId);

    TaxFreePackListOrderItem GetById(long id);
}