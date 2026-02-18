using System;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class AddNewSupplyOrderUkraineFromTaxFreePackListMessage {
    public AddNewSupplyOrderUkraineFromTaxFreePackListMessage(
        SupplyOrderUkraine supplyOrderUkraine,
        Guid packListNetId,
        Guid userNetId
    ) {
        SupplyOrderUkraine = supplyOrderUkraine;

        PackListNetId = packListNetId;

        UserNetId = userNetId;
    }

    public SupplyOrderUkraine SupplyOrderUkraine { get; }

    public Guid PackListNetId { get; }

    public Guid UserNetId { get; }
}