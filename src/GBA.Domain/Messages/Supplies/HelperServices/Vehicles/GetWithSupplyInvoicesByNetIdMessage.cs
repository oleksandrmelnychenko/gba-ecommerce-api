using System;

namespace GBA.Domain.Messages.Supplies.HelperServices.Vehicles;

public sealed class GetWithSupplyInvoicesByNetIdMessage {
    public GetWithSupplyInvoicesByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}