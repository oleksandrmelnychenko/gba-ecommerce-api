using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class UpdatePaymentRegisterMessage {
    public UpdatePaymentRegisterMessage(PaymentRegister paymentRegister) {
        PaymentRegister = paymentRegister;
    }

    public PaymentRegister PaymentRegister { get; set; }
}