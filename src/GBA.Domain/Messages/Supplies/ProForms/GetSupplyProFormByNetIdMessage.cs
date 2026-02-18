using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetSupplyProFormByNetIdMessage {
    public GetSupplyProFormByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}