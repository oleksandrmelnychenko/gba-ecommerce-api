using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Containers;

public sealed class UpdateDeliveryTermMessage {
    public UpdateDeliveryTermMessage(Guid updatedByNetId,
        Guid netId,
        string termDeliveryInDays,
        ContainerService containerService) {
        UpdatedByNetId = updatedByNetId;
        NetId = netId;
        TermDeliveryInDays = termDeliveryInDays;
        ContainerService = containerService;
    }

    public ContainerService ContainerService { get; set; }

    public string TermDeliveryInDays { get; set; }

    public Guid UpdatedByNetId { get; set; }

    public Guid NetId { get; set; }
}