using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class SetActivePaymentRegisterByNetIdMessage {
    public SetActivePaymentRegisterByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}