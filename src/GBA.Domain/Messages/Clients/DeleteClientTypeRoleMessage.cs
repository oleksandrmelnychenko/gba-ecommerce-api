using System;

namespace GBA.Domain.Messages.Clients;

public sealed class DeleteClientTypeRoleMessage {
    public DeleteClientTypeRoleMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}