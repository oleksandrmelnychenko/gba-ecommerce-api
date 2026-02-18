using System;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies.Invoices;

public sealed class AddDocumentsToSupplyInvoiceMessage {
    public AddDocumentsToSupplyInvoiceMessage(
        SupplyInvoice supplyInvoice,
        Guid userNetId) {
        SupplyInvoice = supplyInvoice;
        UserNetId = userNetId;
    }

    public SupplyInvoice SupplyInvoice { get; }
    public Guid UserNetId { get; }
}