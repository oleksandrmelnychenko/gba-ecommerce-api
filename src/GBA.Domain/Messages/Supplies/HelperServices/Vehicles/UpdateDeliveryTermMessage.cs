using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Vehicles;

public sealed class UpdateDeliveryTermMessage {
    public UpdateDeliveryTermMessage(Guid updatedByNetId,
        Guid netId,
        string termDeliveryInDays,
        VehicleService vehicleService) {
        UpdatedByNetId = updatedByNetId;
        NetId = netId;
        TermDeliveryInDays = termDeliveryInDays;
        VehicleService = vehicleService;
    }

    public VehicleService VehicleService { get; set; }

    public string TermDeliveryInDays { get; set; }

    public Guid UpdatedByNetId { get; set; }

    public Guid NetId { get; set; }
}