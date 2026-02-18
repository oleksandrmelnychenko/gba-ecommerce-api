using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetSupplyOrderByNetIdMessage {
    public GetSupplyOrderByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}