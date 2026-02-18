using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.EntityHelpers.Supplies.PackingLists;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IPackingListPackageOrderItemSupplyServiceRepository {
    void New(PackingListPackageOrderItemSupplyService itemService);

    void Update(PackingListPackageOrderItemSupplyService itemService);

    PackingListPackageOrderItemSupplyService GetByPackingListItemAndServiceId(long id, long serviceId, TypeService typeService);

    void RemoveByServiceId(long serviceId, TypeService typeService);
}