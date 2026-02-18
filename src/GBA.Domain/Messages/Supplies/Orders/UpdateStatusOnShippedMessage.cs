using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class UpdateStatusOnShippedMessage {
    public UpdateStatusOnShippedMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}