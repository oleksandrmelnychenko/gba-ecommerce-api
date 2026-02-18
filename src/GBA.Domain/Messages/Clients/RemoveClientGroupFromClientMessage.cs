using System;

namespace GBA.Domain.Messages.Clients;

public sealed class RemoveClientGroupFromClientMessage {
    public RemoveClientGroupFromClientMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}