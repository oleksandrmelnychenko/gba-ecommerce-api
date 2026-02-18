using System;

namespace GBA.Domain.Messages.Clients;

public sealed class DeleteClientTypeMessage {
    public DeleteClientTypeMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}