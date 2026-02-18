using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class GetAllPaymentRegistersByBankMessage {
    public GetAllPaymentRegistersByBankMessage(
        Guid paymentRegisterId) {
        PaymentRegisterId = paymentRegisterId;
    }

    public Guid PaymentRegisterId { get; }
}