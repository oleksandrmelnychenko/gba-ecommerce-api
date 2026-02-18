using System;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.Orders.Depreciated;

public sealed class UpdateNewDepreciatedConsumableOrderMessage {
    public UpdateNewDepreciatedConsumableOrderMessage(DepreciatedConsumableOrder depreciatedConsumableOrder, Guid userNetId, bool expensiveFirst) {
        DepreciatedConsumableOrder = depreciatedConsumableOrder;

        UserNetId = userNetId;

        ExpensiveFirst = expensiveFirst;
    }

    public DepreciatedConsumableOrder DepreciatedConsumableOrder { get; set; }

    public Guid UserNetId { get; set; }

    public bool ExpensiveFirst { get; set; }
}