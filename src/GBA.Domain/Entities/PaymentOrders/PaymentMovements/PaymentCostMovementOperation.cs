using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Entities.PaymentOrders.PaymentMovements;

public sealed class PaymentCostMovementOperation : EntityBase {
    public long PaymentCostMovementId { get; set; }

    public long? ConsumablesOrderItemId { get; set; }

    public long? DepreciatedConsumableOrderItemId { get; set; }

    public long? CompanyCarFuelingId { get; set; }

    public PaymentCostMovement PaymentCostMovement { get; set; }

    public ConsumablesOrderItem ConsumablesOrderItem { get; set; }

    public DepreciatedConsumableOrderItem DepreciatedConsumableOrderItem { get; set; }

    public CompanyCarFueling CompanyCarFueling { get; set; }
}