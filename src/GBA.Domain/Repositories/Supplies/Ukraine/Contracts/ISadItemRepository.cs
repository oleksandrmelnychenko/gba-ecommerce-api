using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISadItemRepository {
    long Add(SadItem item);

    void Add(IEnumerable<SadItem> items);

    void Update(SadItem item);

    void Update(IEnumerable<SadItem> items);

    void RemoveAllBySadIdExceptProvided(long id, IEnumerable<long> ids);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RestoreUnpackedQtyById(long id, double qty);

    void DecreaseUnpackedQtyById(long id, double qty);

    IEnumerable<SadItem> GetAllBySadIdExceptProvided(long sadId, IEnumerable<long> ids);

    IEnumerable<SadItem> GetAllItemsForRemoveBySadIdExceptProvidedItemIds(long sadId, IEnumerable<long> ids);

    SadItem GetById(long id);

    SadItem GetByIdWithoutIncludes(long id);
}