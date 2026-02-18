using System;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class UpdateSupplyOrderUkraineMessage {
    public UpdateSupplyOrderUkraineMessage(
        SupplyOrderUkraine supplyOrderUkraine,
        Guid userNetId
    ) {
        SupplyOrderUkraine = supplyOrderUkraine;

        UserNetId = userNetId;
    }

    public SupplyOrderUkraine SupplyOrderUkraine { get; }

    public Guid UserNetId { get; }
}