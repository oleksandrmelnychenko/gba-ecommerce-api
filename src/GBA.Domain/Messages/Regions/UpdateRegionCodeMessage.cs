using GBA.Domain.Entities.Regions;

namespace GBA.Domain.Messages.Regions;

public sealed class UpdateRegionCodeMessage {
    public UpdateRegionCodeMessage(RegionCode regionCode) {
        RegionCode = regionCode;
    }

    public RegionCode RegionCode { get; set; }
}