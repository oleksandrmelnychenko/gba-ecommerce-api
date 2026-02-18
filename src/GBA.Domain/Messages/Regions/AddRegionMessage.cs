using GBA.Domain.Entities.Regions;

namespace GBA.Domain.Messages.Regions;

public sealed class AddRegionMessage {
    public AddRegionMessage(Region region) {
        Region = region;
    }

    public Region Region { get; set; }
}