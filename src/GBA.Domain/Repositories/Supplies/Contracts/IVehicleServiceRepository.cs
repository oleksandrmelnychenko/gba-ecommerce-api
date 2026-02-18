using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IVehicleServiceRepository {
    long Add(VehicleService vehicleService);

    void Update(VehicleService vehicleService);

    VehicleService GetByNetId(Guid messageNetId);

    VehicleService GetBySupplyOrderVehiclesServiceNetIdWithoutIncludes(Guid netId);

    void SetIsExtraChargeCalculatedByNetId(Guid netId, SupplyExtraChargeType extraChargeType);

    List<VehicleService> GetAllRanged(DateTime from, DateTime to);

    List<VehicleService> GetAllAvailable();

    string GetTermDeliveryInDaysById(long vehicleServiceId);

    void UpdateDeliveryTerms(long vehicleServiceId, string termDeliveryInDays);

    Guid GetSupplyOrderNetIdBySupplyOrderVehicleServiceNetId(Guid messageNetId);

    VehicleService GetByIdWithoutIncludes(long vehicleServiceId);

    bool IsVehicleInOrder(Guid messageSupplyOrderNetId, Guid messageVehicleServiceNetId);

    long GetIdByNetId(Guid messageVehicleServiceNetId);

    void Remove(long vehicleServiceId);

    VehicleService GetById(long id);
}