using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentMovements;

public sealed class DeletePaymentMovementByNetIdMessage {
    public DeletePaymentMovementByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}