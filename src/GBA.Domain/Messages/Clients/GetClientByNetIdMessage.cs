using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetClientByNetIdMessage {
    public GetClientByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}