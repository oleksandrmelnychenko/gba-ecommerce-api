using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class CancelPaymentRegisterTransferByNetIdMessage {
    public CancelPaymentRegisterTransferByNetIdMessage(Guid netId, Guid userNetId) {
        NetId = netId;

        UserNetId = userNetId;
    }

    public Guid NetId { get; set; }

    public Guid UserNetId { get; set; }
}