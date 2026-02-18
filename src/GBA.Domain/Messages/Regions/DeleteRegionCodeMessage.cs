using System;

namespace GBA.Domain.Messages.Regions;

public sealed class DeleteRegionCodeMessage {
    public DeleteRegionCodeMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}