using System;

namespace GBA.Domain.Messages.Clients;

public sealed class DeletePerfectClientMessage {
    public DeletePerfectClientMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}