using System;

namespace GBA.Domain.Messages.Regions;

public sealed class GetRegionByNetIdMessage {
    public GetRegionByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}