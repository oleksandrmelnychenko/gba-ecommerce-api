using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.Sads;

public sealed class GetSadByNetIdMessage {
    public GetSadByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}