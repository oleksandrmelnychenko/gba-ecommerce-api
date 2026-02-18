using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.DynamicProductPlacementRows;

public sealed class UpdateDynamicProductPlacementRowMessage {
    public UpdateDynamicProductPlacementRowMessage(DynamicProductPlacementRow row) {
        Row = row;
    }

    public DynamicProductPlacementRow Row { get; }
}