using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Containers;

public sealed class UpdateContainerServiceDocumentsMessage {
    public UpdateContainerServiceDocumentsMessage(Guid netId, ContainerService containerService) {
        NetId = netId;
        ContainerService = containerService;
    }

    public Guid NetId { get; set; }

    public ContainerService ContainerService { get; set; }
}