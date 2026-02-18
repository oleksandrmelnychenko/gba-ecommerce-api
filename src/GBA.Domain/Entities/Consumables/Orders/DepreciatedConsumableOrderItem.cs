using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.Entities.Consumables;

public sealed class DepreciatedConsumableOrderItem : EntityBase {
    public double Qty { get; set; }

    public decimal TotalPrice { get; set; }

    public long DepreciatedConsumableOrderId { get; set; }

    public long ConsumablesOrderItemId { get; set; }

    public DepreciatedConsumableOrder DepreciatedConsumableOrder { get; set; }

    public ConsumablesOrderItem ConsumablesOrderItem { get; set; }

    public PaymentCostMovementOperation PaymentCostMovementOperation { get; set; }

    public Currency Currency { get; set; }
}