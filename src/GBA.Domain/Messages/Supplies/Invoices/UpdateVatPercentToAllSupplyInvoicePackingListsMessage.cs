using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies.Invoices;

public sealed class UpdateVatPercentToAllSupplyInvoicePackingListsMessage {
    public UpdateVatPercentToAllSupplyInvoicePackingListsMessage(SupplyInvoice supplyInvoice) {
        SupplyInvoice = supplyInvoice;
    }

    public SupplyInvoice SupplyInvoice { get; }
}