using System;

namespace GBA.Domain.Messages.Consumables.Products;

public sealed class DeleteConsumableProductByNetIdMessage {
    public DeleteConsumableProductByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}