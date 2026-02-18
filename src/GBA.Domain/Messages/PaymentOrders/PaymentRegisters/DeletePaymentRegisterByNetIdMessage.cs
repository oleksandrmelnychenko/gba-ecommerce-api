using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class DeletePaymentRegisterByNetIdMessage {
    public DeletePaymentRegisterByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}