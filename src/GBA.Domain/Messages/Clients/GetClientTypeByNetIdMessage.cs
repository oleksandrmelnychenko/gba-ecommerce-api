using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetClientTypeByNetIdMessage {
    public GetClientTypeByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}