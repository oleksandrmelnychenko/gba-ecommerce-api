using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class AddOrUpdatePackingListsMessage {
    public AddOrUpdatePackingListsMessage(SupplyInvoice supplyInvoice) {
        SupplyInvoice = supplyInvoice;
    }

    public SupplyInvoice SupplyInvoice { get; set; }
}