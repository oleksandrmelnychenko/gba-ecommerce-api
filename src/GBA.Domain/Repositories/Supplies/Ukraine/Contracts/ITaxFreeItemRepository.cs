using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ITaxFreeItemRepository {
    long Add(TaxFreeItem item);

    void Add(IEnumerable<TaxFreeItem> items);

    void Update(TaxFreeItem item);

    void Update(IEnumerable<TaxFreeItem> items);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByOrderItemIds(IEnumerable<long> ids);

    void RemoveAllByTaxFreeIdExceptProvided(long taxFreeId, IEnumerable<long> ids);

    void RemoveAllByPackListAndCartItemIds(long taxFreePackListId, IEnumerable<long> deletedItemIds);

    TaxFreeItem GetById(long id);

    TaxFreeItem GetByTaxFreeAndCartItemIdsIfExists(long taxFreeId, long cartItemId);

    TaxFreeItem GetByTaxFreeAndPackListOrderItemIdsIfExists(long taxFreeId, long packListOrderItemId);

    List<TaxFreeItem> GetAllByTaxFreeId(long taxFreeId);

    List<TaxFreeItem> GetAllByTaxFreeIdExceptProvided(long taxFreeId, IEnumerable<long> ids);
}