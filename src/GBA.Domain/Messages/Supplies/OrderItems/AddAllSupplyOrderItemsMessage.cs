using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies;

public sealed class AddAllSupplyOrderItemsMessage {
    public AddAllSupplyOrderItemsMessage(IEnumerable<SupplyOrderItem> supplyOrderItems) {
        SupplyOrderItems = supplyOrderItems;
    }

    public IEnumerable<SupplyOrderItem> SupplyOrderItems { get; set; }
}