using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies;

public sealed class AddOrUpdateSupplyInvoiceOrderItemsMessage {
    public AddOrUpdateSupplyInvoiceOrderItemsMessage(SupplyInvoice supplyInvoice) {
        SupplyInvoice = supplyInvoice;
    }

    public SupplyInvoice SupplyInvoice { get; set; }
}