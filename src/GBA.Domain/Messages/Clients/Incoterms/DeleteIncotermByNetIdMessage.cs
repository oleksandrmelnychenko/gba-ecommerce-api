using System;

namespace GBA.Domain.Messages.Clients.Incoterms;

public sealed class DeleteIncotermByNetIdMessage {
    public DeleteIncotermByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}