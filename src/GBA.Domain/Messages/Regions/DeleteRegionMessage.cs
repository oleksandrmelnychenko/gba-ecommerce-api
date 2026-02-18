using System;

namespace GBA.Domain.Messages.Regions;

public sealed class DeleteRegionMessage {
    public DeleteRegionMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}