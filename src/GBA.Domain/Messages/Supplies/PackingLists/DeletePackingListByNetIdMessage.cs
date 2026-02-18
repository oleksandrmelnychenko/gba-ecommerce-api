using System;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class DeletePackingListByNetIdMessage {
    public DeletePackingListByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}