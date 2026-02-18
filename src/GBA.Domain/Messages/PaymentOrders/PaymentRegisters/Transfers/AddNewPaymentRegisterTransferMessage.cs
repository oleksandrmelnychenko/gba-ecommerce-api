using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class AddNewPaymentRegisterTransferMessage {
    public AddNewPaymentRegisterTransferMessage(PaymentRegisterTransfer paymentRegisterTransfer, Guid userNetId) {
        PaymentRegisterTransfer = paymentRegisterTransfer;

        UserNetId = userNetId;
    }

    public PaymentRegisterTransfer PaymentRegisterTransfer { get; set; }

    public Guid UserNetId { get; set; }
}