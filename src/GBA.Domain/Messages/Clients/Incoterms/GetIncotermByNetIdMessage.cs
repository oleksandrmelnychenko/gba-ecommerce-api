using System;

namespace GBA.Domain.Messages.Clients.Incoterms;

public sealed class GetIncotermByNetIdMessage {
    public GetIncotermByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}