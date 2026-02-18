using System;

namespace GBA.Domain.Messages.Clients;

public sealed class SwitchActiveClientStateMessage {
    public SwitchActiveClientStateMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}