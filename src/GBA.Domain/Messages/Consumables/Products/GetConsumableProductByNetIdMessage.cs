using System;

namespace GBA.Domain.Messages.Consumables.Products;

public sealed class GetConsumableProductByNetIdMessage {
    public GetConsumableProductByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}