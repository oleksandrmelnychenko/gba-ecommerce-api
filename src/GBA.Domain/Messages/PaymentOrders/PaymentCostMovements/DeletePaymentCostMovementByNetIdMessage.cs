using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentCostMovements;

public sealed class DeletePaymentCostMovementByNetIdMessage {
    public DeletePaymentCostMovementByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}