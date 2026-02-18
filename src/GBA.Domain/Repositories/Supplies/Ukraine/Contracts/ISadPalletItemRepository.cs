using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISadPalletItemRepository {
    long Add(SadPalletItem sadPalletItem);

    void Update(SadPalletItem sadPalletItem);

    SadPalletItem GetById(long id);

    SadPalletItem GetByPalletAndSadItemIdIfExists(long palletId, long sadItemId);

    void RestoreUnpackedQtyByPalletIdExceptProvidedIds(long sadId, IEnumerable<long> ids);

    void RemoveAllByPalletIdExceptProvided(long palletId, IEnumerable<long> ids);
}