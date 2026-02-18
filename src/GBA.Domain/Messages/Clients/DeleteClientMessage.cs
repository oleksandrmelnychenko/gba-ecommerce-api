using System;

namespace GBA.Domain.Messages.Clients;

public sealed class DeleteClientMessage {
    public DeleteClientMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}