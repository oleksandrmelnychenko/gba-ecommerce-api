using System;

namespace GBA.Domain.Messages.Supplies.HelperServices.Vehicles;

public sealed class UnassigningVehicleServiceBeforeCalculatedExtraChargeMessage {
    public UnassigningVehicleServiceBeforeCalculatedExtraChargeMessage(
        Guid supplyOrderNetId,
        Guid vehicleServiceNetId) {
        SupplyOrderNetId = supplyOrderNetId;

        VehicleServiceNetId = vehicleServiceNetId;
    }

    public Guid SupplyOrderNetId { get; }

    public Guid VehicleServiceNetId { get; }
}