using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetTotalsBySupplyOrderNetIdMessage {
    public GetTotalsBySupplyOrderNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}