using System;

namespace GBA.Domain.Messages.Consumables.Storages;

public sealed class GetConsumablesStorageByNetIdMessage {
    public GetConsumablesStorageByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}