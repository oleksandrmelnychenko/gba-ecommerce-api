using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetWorkplacesByGroupNetIdMessage {
    public GetWorkplacesByGroupNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}