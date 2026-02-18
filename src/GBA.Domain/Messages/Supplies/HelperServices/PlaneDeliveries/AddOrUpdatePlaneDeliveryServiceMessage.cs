using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.PlaneDeliveries;

public sealed class AddOrUpdatePlaneDeliveryServiceMessage {
    public AddOrUpdatePlaneDeliveryServiceMessage(Guid netId, PlaneDeliveryService planeDeliveryService) {
        PlaneDeliveryService = planeDeliveryService;
        NetId = netId;
    }

    public Guid NetId { get; set; }

    public PlaneDeliveryService PlaneDeliveryService { get; set; }
}