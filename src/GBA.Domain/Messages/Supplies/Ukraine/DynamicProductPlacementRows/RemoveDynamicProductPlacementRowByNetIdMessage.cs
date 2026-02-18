using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.DynamicProductPlacementRows;

public sealed class RemoveDynamicProductPlacementRowByNetIdMessage {
    public RemoveDynamicProductPlacementRowByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}