using System;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.Orders;

public sealed class UpdateConsumablesOrderMessage {
    public UpdateConsumablesOrderMessage(ConsumablesOrder consumablesOrder, Guid userNetId) {
        ConsumablesOrder = consumablesOrder;

        UserNetId = userNetId;
    }

    public ConsumablesOrder ConsumablesOrder { get; set; }

    public Guid UserNetId { get; set; }
}