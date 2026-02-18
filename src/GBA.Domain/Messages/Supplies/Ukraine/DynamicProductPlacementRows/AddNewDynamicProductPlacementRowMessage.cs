using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.DynamicProductPlacementRows;

public sealed class AddNewDynamicProductPlacementRowMessage {
    public AddNewDynamicProductPlacementRowMessage(DynamicProductPlacementRow row) {
        Row = row;
    }

    public DynamicProductPlacementRow Row { get; }
}