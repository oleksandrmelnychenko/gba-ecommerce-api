using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetAllSupplyDeliveryDocumentsByTypeMessage {
    public GetAllSupplyDeliveryDocumentsByTypeMessage(SupplyTransportationType type) {
        Type = type;
    }

    public SupplyTransportationType Type { get; set; }
}