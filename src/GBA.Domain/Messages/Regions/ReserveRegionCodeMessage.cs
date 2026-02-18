using GBA.Domain.Entities.Regions;

namespace GBA.Domain.Messages.Regions;

public sealed class ReserveRegionCodeMessage {
    public ReserveRegionCodeMessage(Region region) {
        Region = region;
    }

    public Region Region { get; set; }
}