using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class SetSupplyOrderUkraineIsPlacedByNetIdMessage {
    public SetSupplyOrderUkraineIsPlacedByNetIdMessage(Guid netId, Guid userNetId) {
        NetId = netId;

        UserNetId = userNetId;
    }

    public Guid NetId { get; }

    public Guid UserNetId { get; }
}