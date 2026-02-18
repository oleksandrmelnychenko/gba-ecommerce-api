using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Entities.PaymentOrders;

public sealed class OutcomePaymentOrderSupplyPaymentTask : EntityBase {
    public decimal Amount { get; set; }

    public long OutcomePaymentOrderId { get; set; }

    public long SupplyPaymentTaskId { get; set; }

    public OutcomePaymentOrder OutcomePaymentOrder { get; set; }

    public SupplyPaymentTask SupplyPaymentTask { get; set; }
}