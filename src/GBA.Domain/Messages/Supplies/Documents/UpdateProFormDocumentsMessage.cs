using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies.Documents;

public sealed class UpdateProFormDocumentsMessage {
    public UpdateProFormDocumentsMessage(SupplyProForm supplyProForm) {
        SupplyProForm = supplyProForm;
    }

    public SupplyProForm SupplyProForm { get; set; }
}