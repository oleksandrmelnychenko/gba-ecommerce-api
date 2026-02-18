using System;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class SetReadyToPlaceToPackingListPackageOrderItemByNetIdMessage {
    public SetReadyToPlaceToPackingListPackageOrderItemByNetIdMessage(Guid itemNetId, bool toSetValue) {
        ItemNetId = itemNetId;

        ToSetValue = toSetValue;
    }

    public Guid ItemNetId { get; }

    public bool ToSetValue { get; }
}