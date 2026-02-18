using System;

namespace GBA.Domain.Messages.OriginalNumbers;

public sealed class GetOriginalNumberByNetIdMessage {
    public GetOriginalNumberByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}