using System;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies.Documents;

public sealed class UpdateInvoiceDocumentsMessage {
    public UpdateInvoiceDocumentsMessage(Guid netId, SupplyInvoice supplyInvoice) {
        NetId = netId;
        SupplyInvoice = supplyInvoice;
    }

    public Guid NetId { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }
}