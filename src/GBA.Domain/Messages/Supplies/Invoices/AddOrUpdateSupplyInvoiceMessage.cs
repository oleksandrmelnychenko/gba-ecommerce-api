using System;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies;

public sealed class AddOrUpdateSupplyInvoiceMessage {
    public AddOrUpdateSupplyInvoiceMessage(Guid supplyOrderNetId, SupplyInvoice supplyInvoice) {
        SupplyOrderNetId = supplyOrderNetId;

        SupplyInvoice = supplyInvoice;
    }

    public Guid SupplyOrderNetId { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }
}