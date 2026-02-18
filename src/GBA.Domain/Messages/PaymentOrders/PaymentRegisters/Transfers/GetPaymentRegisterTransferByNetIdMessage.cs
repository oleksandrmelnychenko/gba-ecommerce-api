using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class GetPaymentRegisterTransferByNetIdMessage {
    public GetPaymentRegisterTransferByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}