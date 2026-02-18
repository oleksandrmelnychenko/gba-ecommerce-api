using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IPackingListPackageRepository {
    long Add(PackingListPackage packingListPallet);

    void Add(IEnumerable<PackingListPackage> packingListPallets);

    void Update(PackingListPackage packingListPallet);

    void Update(IEnumerable<PackingListPackage> packingListPallets);

    void RemoveAllByPackingListIdExceptProvided(long packingListId, IEnumerable<long> boxIds, IEnumerable<long> palletIds);

    void UpdateRemainingQty(PackingListPackageOrderItem item);
}