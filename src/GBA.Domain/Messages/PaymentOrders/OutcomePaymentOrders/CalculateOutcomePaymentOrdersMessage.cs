using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;

public sealed class CalculateOutcomePaymentOrdersMessage {
    public CalculateOutcomePaymentOrdersMessage(OutcomePaymentOrder outcomePaymentOrder) {
        OutcomePaymentOrder = outcomePaymentOrder;
    }

    public OutcomePaymentOrder OutcomePaymentOrder { get; set; }
}