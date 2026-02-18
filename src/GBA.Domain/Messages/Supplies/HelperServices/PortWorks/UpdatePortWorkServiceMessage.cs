using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.PortWorks;

public sealed class UpdatePortWorkServiceMessage {
    public UpdatePortWorkServiceMessage(Guid netId, PortWorkService portWorkService) {
        NetId = netId;
        PortWorkService = portWorkService;
    }

    public Guid NetId { get; set; }

    public PortWorkService PortWorkService { get; set; }
}