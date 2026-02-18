using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetTotalsOnSupplyOrderItemsBySupplyOrderNetIdMessage {
    public GetTotalsOnSupplyOrderItemsBySupplyOrderNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}