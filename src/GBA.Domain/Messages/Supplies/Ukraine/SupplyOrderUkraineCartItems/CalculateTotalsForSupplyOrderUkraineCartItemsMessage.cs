using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.SupplyOrderUkraineCartItems;

public sealed class CalculateTotalsForSupplyOrderUkraineCartItemsMessage {
    public CalculateTotalsForSupplyOrderUkraineCartItemsMessage(IEnumerable<SupplyOrderUkraineCartItem> items) {
        Items = items;
    }

    public IEnumerable<SupplyOrderUkraineCartItem> Items { get; }
}