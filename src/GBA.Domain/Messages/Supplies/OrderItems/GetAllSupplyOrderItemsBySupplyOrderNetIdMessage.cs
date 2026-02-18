using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetAllSupplyOrderItemsBySupplyOrderNetIdMessage {
    public GetAllSupplyOrderItemsBySupplyOrderNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}