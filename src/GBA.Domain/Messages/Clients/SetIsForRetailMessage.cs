using System;

namespace GBA.Domain.Messages.Clients;

public sealed class SetIsForRetailMessage {
    public SetIsForRetailMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}