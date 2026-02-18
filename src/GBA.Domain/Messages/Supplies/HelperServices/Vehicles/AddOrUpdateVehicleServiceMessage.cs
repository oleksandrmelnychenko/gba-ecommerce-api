using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Vehicles;

public sealed class AddOrUpdateVehicleServiceMessage {
    public AddOrUpdateVehicleServiceMessage(Guid netId, VehicleService vehicleService) {
        NetId = netId;

        VehicleService = vehicleService;
    }

    public Guid NetId { get; set; }

    public VehicleService VehicleService { get; set; }
}