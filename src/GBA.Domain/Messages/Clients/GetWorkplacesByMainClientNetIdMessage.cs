using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetWorkplacesByMainClientNetIdMessage {
    public GetWorkplacesByMainClientNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}