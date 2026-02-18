using System;

namespace GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;

public sealed class CancelOutcomePaymentOrderByNetIdMessage {
    public CancelOutcomePaymentOrderByNetIdMessage(Guid netId, Guid userNetId) {
        NetId = netId;
        UserNetId = userNetId;
    }

    public Guid NetId { get; }
    public Guid UserNetId { get; }
}