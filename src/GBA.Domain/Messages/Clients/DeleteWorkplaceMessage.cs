using System;

namespace GBA.Domain.Messages.Clients;

public sealed class DeleteWorkplaceMessage {
    public DeleteWorkplaceMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}