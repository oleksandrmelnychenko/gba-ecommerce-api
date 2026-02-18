using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.Messages.PaymentOrders.PaymentMovements;

public sealed class UpdatePaymentMovementMessage {
    public UpdatePaymentMovementMessage(PaymentMovement paymentMovement) {
        PaymentMovement = paymentMovement;
    }

    public PaymentMovement PaymentMovement { get; set; }
}