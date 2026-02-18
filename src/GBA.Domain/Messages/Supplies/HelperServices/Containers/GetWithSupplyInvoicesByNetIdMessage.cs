using System;

namespace GBA.Domain.Messages.Supplies.HelperServices.Containers;

public sealed class GetWithSupplyInvoicesByNetIdMessage {
    public GetWithSupplyInvoicesByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}