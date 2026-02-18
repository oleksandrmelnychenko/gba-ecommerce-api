namespace GBA.Domain.Entities.Consumables.Orders;

public sealed class ConsumablesOrderDocument : BaseDocument {
    public long ConsumablesOrderId { get; set; }

    public ConsumablesOrder ConsumablesOrder { get; set; }
}