using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetAllServiceDetailItemKeysByServiceTypeMessage {
    public GetAllServiceDetailItemKeysByServiceTypeMessage(SupplyServiceType supplyServiceType) {
        SupplyServiceType = supplyServiceType;
    }

    public SupplyServiceType SupplyServiceType { get; set; }
}