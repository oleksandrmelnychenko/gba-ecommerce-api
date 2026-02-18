using System;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class GetAllPackingListsBySupplyInvoiceNetIdMessage {
    public GetAllPackingListsBySupplyInvoiceNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}