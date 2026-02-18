using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.PortWorks;

public sealed class AddPortWorkServiceMessage {
    public AddPortWorkServiceMessage(
        PortWorkService portWorkService,
        Guid userNetId) {
        PortWorkService = portWorkService;

        UserNetId = userNetId;
    }

    public PortWorkService PortWorkService { get; set; }

    public Guid UserNetId { get; }
}