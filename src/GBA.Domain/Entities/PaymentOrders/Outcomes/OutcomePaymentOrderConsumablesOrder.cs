using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Entities.PaymentOrders;

public sealed class OutcomePaymentOrderConsumablesOrder : EntityBase {
    public long OutcomePaymentOrderId { get; set; }

    public long ConsumablesOrderId { get; set; }

    public OutcomePaymentOrder OutcomePaymentOrder { get; set; }

    public ConsumablesOrder ConsumablesOrder { get; set; }
}