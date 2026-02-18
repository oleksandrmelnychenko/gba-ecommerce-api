using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentCostMovements;

public sealed class GetPaymentCostMovementByNetIdMessage {
    public GetPaymentCostMovementByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}