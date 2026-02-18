using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IVehicleDeliveryServiceRepository {
    long Add(VehicleDeliveryService vehicleDeliveryService);

    void Update(VehicleDeliveryService vehicleDeliveryService);

    VehicleDeliveryService GetById(long id);

    List<VehicleDeliveryService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    VehicleDeliveryService GetByIdWithoutIncludes(long id);
}