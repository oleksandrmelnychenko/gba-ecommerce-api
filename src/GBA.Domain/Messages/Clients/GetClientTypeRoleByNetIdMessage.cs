using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetClientTypeRoleByNetIdMessage {
    public GetClientTypeRoleByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}