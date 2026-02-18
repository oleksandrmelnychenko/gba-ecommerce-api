using System;

namespace GBA.Domain.Messages.ReSales;

public sealed class ChangeIsCompletedMessage {
    public ChangeIsCompletedMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}