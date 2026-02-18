using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrderVehicleServiceRepository {
    void RemoveAllBySupplyOrderId(long supplyOrderId);

    void RemoveAllBySupplyOrderIdExceptProvided(long supplyOrderId, IEnumerable<long> select);

    void Add(IEnumerable<SupplyOrderVehicleService> supplyOrderVehicleServices);

    void Update(IEnumerable<SupplyOrderVehicleService> supplyOrderVehicleServices);

    void RemoveAllBySupplyOrderAndVehicleServiceId(long supplyOrderId, long vehicleServiceId);

    void RemoveAllByVehicleServiceId(long vehicleServiceId);
}