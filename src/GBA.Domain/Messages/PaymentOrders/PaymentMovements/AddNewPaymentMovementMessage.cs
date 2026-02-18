using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.Messages.PaymentOrders.PaymentMovements;

public sealed class AddNewPaymentMovementMessage {
    public AddNewPaymentMovementMessage(PaymentMovement paymentMovement) {
        PaymentMovement = paymentMovement;
    }

    public PaymentMovement PaymentMovement { get; set; }
}