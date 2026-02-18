using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.PlaneDeliveries;

public sealed class UpdatePlaneDeliveryServiceMessage {
    public UpdatePlaneDeliveryServiceMessage(Guid netId, PlaneDeliveryService planeDeliveryService) {
        NetId = netId;
        PlaneDeliveryService = planeDeliveryService;
    }

    public Guid NetId { get; set; }

    public PlaneDeliveryService PlaneDeliveryService { get; set; }
}