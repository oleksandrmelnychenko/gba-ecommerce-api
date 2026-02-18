using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Vehicles;

public sealed class UpdateVehicleServiceDocumentsMessage {
    public UpdateVehicleServiceDocumentsMessage(Guid netId, VehicleService vehicleService) {
        NetId = netId;
        VehicleService = vehicleService;
    }

    public Guid NetId { get; set; }

    public VehicleService VehicleService { get; set; }
}