using System;

namespace GBA.Domain.Messages.Consumables.Orders;

public sealed class DeleteConsumablesOrderByNetIdMessage {
    public DeleteConsumablesOrderByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}