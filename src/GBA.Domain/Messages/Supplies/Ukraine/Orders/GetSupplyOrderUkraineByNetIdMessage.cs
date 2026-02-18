using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class GetSupplyOrderUkraineByNetIdMessage {
    public GetSupplyOrderUkraineByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}