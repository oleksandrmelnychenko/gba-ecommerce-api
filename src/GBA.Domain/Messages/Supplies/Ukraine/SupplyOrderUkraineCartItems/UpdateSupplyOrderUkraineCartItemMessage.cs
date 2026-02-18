using System;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.SupplyOrderUkraineCartItems;

public sealed class UpdateSupplyOrderUkraineCartItemMessage {
    public UpdateSupplyOrderUkraineCartItemMessage(
        SupplyOrderUkraineCartItem item,
        Guid userNetId
    ) {
        Item = item;

        UserNetId = userNetId;
    }

    public SupplyOrderUkraineCartItem Item { get; }

    public Guid UserNetId { get; }
}