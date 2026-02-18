using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies;

public sealed class AddSupplyDeliveryDocumentMessage {
    public AddSupplyDeliveryDocumentMessage(SupplyDeliveryDocument supplyDeliveryDocument) {
        SupplyDeliveryDocument = supplyDeliveryDocument;
    }

    public SupplyDeliveryDocument SupplyDeliveryDocument { get; set; }
}