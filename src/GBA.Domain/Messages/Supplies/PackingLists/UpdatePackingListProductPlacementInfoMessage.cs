using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class UpdatePackingListProductPlacementInfoMessage {
    public UpdatePackingListProductPlacementInfoMessage(PackingList packingList) {
        PackingList = packingList;
    }

    public PackingList PackingList { get; }
}