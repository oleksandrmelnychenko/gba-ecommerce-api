using System;

namespace GBA.Domain.Messages.ReSales;

public sealed class RemoveReSaleMessage {
    public RemoveReSaleMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}