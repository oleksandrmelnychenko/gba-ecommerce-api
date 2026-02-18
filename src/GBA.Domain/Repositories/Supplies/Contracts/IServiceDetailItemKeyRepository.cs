using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IServiceDetailItemKeyRepository {
    long Add(ServiceDetailItemKey key);

    void Update(ServiceDetailItemKey key);

    void Update(IEnumerable<ServiceDetailItemKey> keys);

    ServiceDetailItemKey GetByFieldsIfExists(string name, string symbol, SupplyServiceType type);

    List<ServiceDetailItemKey> GetAllByType(SupplyServiceType type);
}