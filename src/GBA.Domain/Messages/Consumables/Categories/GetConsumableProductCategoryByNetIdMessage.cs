using System;

namespace GBA.Domain.Messages.Consumables.Categories;

public sealed class GetConsumableProductCategoryByNetIdMessage {
    public GetConsumableProductCategoryByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}