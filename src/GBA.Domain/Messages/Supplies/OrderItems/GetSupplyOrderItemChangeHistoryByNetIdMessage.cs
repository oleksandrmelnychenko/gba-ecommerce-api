using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetSupplyOrderItemChangeHistoryByNetIdMessage {
    public GetSupplyOrderItemChangeHistoryByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}