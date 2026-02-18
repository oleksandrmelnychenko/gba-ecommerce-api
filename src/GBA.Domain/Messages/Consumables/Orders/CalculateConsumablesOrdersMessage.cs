using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.Orders;

public sealed class CalculateConsumablesOrdersMessage {
    public CalculateConsumablesOrdersMessage(IEnumerable<ConsumablesOrder> consumablesOrders) {
        ConsumablesOrders = consumablesOrders;
    }

    public IEnumerable<ConsumablesOrder> ConsumablesOrders { get; set; }
}