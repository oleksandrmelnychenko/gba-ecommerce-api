using GBA.Domain.Entities.Regions;

namespace GBA.Domain.Messages.Regions;

public sealed class UpdateRegionMessage {
    public UpdateRegionMessage(Region region) {
        Region = region;
    }

    public Region Region { get; set; }
}