using System;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class UpdateVatsForPackingListMessage {
    public UpdateVatsForPackingListMessage(
        PackingList packingList,
        Guid userNetId) {
        PackingList = packingList;
        UserNetId = userNetId;
    }

    public PackingList PackingList { get; }
    public Guid UserNetId { get; }
}