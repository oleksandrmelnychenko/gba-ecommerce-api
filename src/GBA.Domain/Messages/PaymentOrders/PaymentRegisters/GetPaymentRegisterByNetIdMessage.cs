using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class GetPaymentRegisterByNetIdMessage {
    public GetPaymentRegisterByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}