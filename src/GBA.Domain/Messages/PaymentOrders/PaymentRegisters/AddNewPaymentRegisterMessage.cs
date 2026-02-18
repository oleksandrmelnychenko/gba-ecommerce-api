using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class AddNewPaymentRegisterMessage {
    public AddNewPaymentRegisterMessage(PaymentRegister paymentRegister) {
        PaymentRegister = paymentRegister;
    }

    public PaymentRegister PaymentRegister { get; set; }
}