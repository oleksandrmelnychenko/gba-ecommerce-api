using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class UpdateSupplyOrderUkraineItemMessage {
    public UpdateSupplyOrderUkraineItemMessage(
        Guid netId,
        IEnumerable<SupplyOrderUkraineItem> supplyOrderUkraineItems) {
        NetId = netId;
        SupplyOrderUkraineItems = supplyOrderUkraineItems;
    }

    public Guid NetId { get; }
    public IEnumerable<SupplyOrderUkraineItem> SupplyOrderUkraineItems { get; }
}