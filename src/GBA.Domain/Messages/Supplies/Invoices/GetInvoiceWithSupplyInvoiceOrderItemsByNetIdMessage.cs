using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetInvoiceWithSupplyInvoiceOrderItemsByNetIdMessage {
    public GetInvoiceWithSupplyInvoiceOrderItemsByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}