using System;

namespace GBA.Domain.Messages.DepreciatedOrders;

public sealed class GetDepreciatedOrderByNetIdMessage {
    public GetDepreciatedOrderByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}