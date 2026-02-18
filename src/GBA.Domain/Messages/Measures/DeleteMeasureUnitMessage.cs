using System;

namespace GBA.Domain.Messages.Measures;

public sealed class DeleteMeasureUnitMessage {
    public DeleteMeasureUnitMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}