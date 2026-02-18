using System;

namespace GBA.Domain.Messages.Consumables.Storages;

public sealed class DeleteConsumablesStorageByNetIdMessage {
    public DeleteConsumablesStorageByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}