using System;

namespace GBA.Domain.Messages.OriginalNumbers;

public sealed class DeleteOriginalNumberMessage {
    public DeleteOriginalNumberMessage(
        Guid netId,
        Guid productNetId) {
        NetId = netId;
        ProductNetId = productNetId;
    }

    public Guid NetId { get; set; }

    public Guid ProductNetId { get; }
}