using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetPerfectClientByNetIdMessage {
    public GetPerfectClientByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}