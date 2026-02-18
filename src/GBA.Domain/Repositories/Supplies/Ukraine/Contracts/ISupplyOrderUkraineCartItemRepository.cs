using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISupplyOrderUkraineCartItemRepository {
    long Add(SupplyOrderUkraineCartItem item);

    void Add(IEnumerable<SupplyOrderUkraineCartItem> items);

    void Update(SupplyOrderUkraineCartItem item);

    void Update(IEnumerable<SupplyOrderUkraineCartItem> items);

    void Remove(long id);

    void UpdateUnpackedQty(long id, double qty);

    void UpdateMaxQtyPerTf(SupplyOrderUkraineCartItem item);

    void ReturnItemsToCartFromTaxFreePackListExceptProvided(long packListId, long updatedById, IEnumerable<long> ids);

    void RestoreUnpackedQtyByTaxFreeItemsIds(IEnumerable<long> ids);

    void RestoreUnpackedQtyByTaxFreeIdExceptProvidedIds(long taxFreeId, IEnumerable<long> ids);

    void DecreaseUnpackedQtyById(long id, double qty);

    SupplyOrderUkraineCartItem GetByProductIdIfExists(long id);

    SupplyOrderUkraineCartItem GetById(long id);

    SupplyOrderUkraineCartItem GetByIdWithoutMovementInfo(long id);

    SupplyOrderUkraineCartItem GetByProductIdIfReserved(long productId);

    SupplyOrderUkraineCartItem GetByIdWithReservations(long id);

    SupplyOrderUkraineCartItem GetByProductAndTaxFreePackListIdsIfExists(long productId, long packListId);

    SupplyOrderUkraineCartItem GetAssignedItemByTaxFreePackListAndConsignmentItemIfExists(long taxFreePackListId, long consignmentItemId);

    IEnumerable<SupplyOrderUkraineCartItem> GetAll();

    IEnumerable<SupplyOrderUkraineCartItem> GetAllByPackListIdExceptProvided(long packListId, IEnumerable<long> ids);
}