using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetNearestSupplyArrivalByProductNetIdMessage {
    public GetNearestSupplyArrivalByProductNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}