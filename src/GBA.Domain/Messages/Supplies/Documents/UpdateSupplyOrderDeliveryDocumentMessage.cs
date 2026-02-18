using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Messages.Supplies.Documents;

public sealed class UpdateSupplyOrderDeliveryDocumentMessage {
    public UpdateSupplyOrderDeliveryDocumentMessage(SupplyOrderDeliveryDocument document) {
        Document = document;
    }

    public SupplyOrderDeliveryDocument Document { get; set; }
}