using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetSupplyInvoiceByNetIdMessage {
    public GetSupplyInvoiceByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}