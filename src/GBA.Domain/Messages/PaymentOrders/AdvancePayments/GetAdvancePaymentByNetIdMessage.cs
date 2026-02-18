using System;

namespace GBA.Domain.Messages.PaymentOrders.AdvancePayments;

public sealed class GetAdvancePaymentByNetIdMessage {
    public GetAdvancePaymentByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}