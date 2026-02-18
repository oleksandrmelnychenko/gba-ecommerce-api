using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSaleByNetIdWithShiftedItemsMessage {
    public GetSaleByNetIdWithShiftedItemsMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}