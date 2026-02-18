using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetSupplyOrderByNetIdForPlacementMessage {
    public GetSupplyOrderByNetIdForPlacementMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}