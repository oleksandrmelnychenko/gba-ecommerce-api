using System;

namespace GBA.Domain.Messages.Consumables.Orders;

public sealed class GetConsumablesOrderByNetIdMessage {
    public GetConsumablesOrderByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}