using System;

namespace GBA.Domain.Messages.Consumables.Orders.Depreciated;

public sealed class DeleteDepreciatedConsumableOrderByNetIdMessage {
    public DeleteDepreciatedConsumableOrderByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}