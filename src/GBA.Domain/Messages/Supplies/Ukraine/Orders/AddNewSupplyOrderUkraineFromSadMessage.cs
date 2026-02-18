using System;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class AddNewSupplyOrderUkraineFromSadMessage {
    public AddNewSupplyOrderUkraineFromSadMessage(
        SupplyOrderUkraine supplyOrderUkraine,
        Guid sadNetId,
        Guid userNetId
    ) {
        SupplyOrderUkraine = supplyOrderUkraine;

        SadNetId = sadNetId;

        UserNetId = userNetId;
    }

    public SupplyOrderUkraine SupplyOrderUkraine { get; }

    public Guid SadNetId { get; }

    public Guid UserNetId { get; }
}