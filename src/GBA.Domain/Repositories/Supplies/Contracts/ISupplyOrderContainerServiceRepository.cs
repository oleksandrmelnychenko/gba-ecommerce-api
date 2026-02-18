using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrderContainerServiceRepository {
    void Add(IEnumerable<SupplyOrderContainerService> supplyOrderContainerServices);

    void Update(IEnumerable<SupplyOrderContainerService> supplyOrderContainerServices);

    void RemoveAllBySupplyOrderId(long id);

    void RemoveAllBySupplyOrderIdExceptProvided(long id, IEnumerable<long> ids);

    void RemoveAllBySupplyOrderAndContainerServiceId(long supplyOrderId, long containerServiceId);

    void RemoveAllByContainerServiceId(long containerServiceId);
}