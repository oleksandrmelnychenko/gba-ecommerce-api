using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.VehicleDeliveries;

public sealed class InsertOrUpdateVehicleDeliveryServiceMessage {
    public InsertOrUpdateVehicleDeliveryServiceMessage(Guid netId, VehicleDeliveryService vehicleDeliveryService, Guid userNetId) {
        NetId = netId;

        VehicleDeliveryService = vehicleDeliveryService;

        UserNetId = userNetId;
    }

    public Guid NetId { get; set; }

    public VehicleDeliveryService VehicleDeliveryService { get; set; }

    public Guid UserNetId { get; }
}