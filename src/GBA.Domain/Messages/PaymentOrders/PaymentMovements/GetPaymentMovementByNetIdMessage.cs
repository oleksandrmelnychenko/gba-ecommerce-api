using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentMovements;

public sealed class GetPaymentMovementByNetIdMessage {
    public GetPaymentMovementByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}