using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.Messages.PaymentOrders.PaymentCostMovements;

public sealed class UpdatePaymentCostMovementMessage {
    public UpdatePaymentCostMovementMessage(PaymentCostMovement paymentCostMovement) {
        PaymentCostMovement = paymentCostMovement;
    }

    public PaymentCostMovement PaymentCostMovement { get; set; }
}