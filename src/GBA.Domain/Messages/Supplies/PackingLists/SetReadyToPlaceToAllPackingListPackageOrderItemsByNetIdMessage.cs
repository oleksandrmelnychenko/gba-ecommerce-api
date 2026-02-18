using System;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class SetReadyToPlaceToAllPackingListPackageOrderItemsByNetIdMessage {
    public SetReadyToPlaceToAllPackingListPackageOrderItemsByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}