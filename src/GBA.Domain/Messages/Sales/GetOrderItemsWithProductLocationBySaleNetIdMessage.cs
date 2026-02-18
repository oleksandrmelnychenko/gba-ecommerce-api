using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetOrderItemsWithProductLocationBySaleNetIdMessage {
    public GetOrderItemsWithProductLocationBySaleNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}