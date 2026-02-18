using System;

namespace GBA.Domain.Messages.Agreements;

public sealed class DeleteCalculationTypeMessage {
    public DeleteCalculationTypeMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}