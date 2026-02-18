using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public class SetSelectedPaymentRegisterByNetId {
    public SetSelectedPaymentRegisterByNetId(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}