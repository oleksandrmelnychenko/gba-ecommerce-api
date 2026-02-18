using System;

namespace GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;

public sealed class CancelIncomePaymentOrderByNetIdMessage {
    public CancelIncomePaymentOrderByNetIdMessage(Guid netId, Guid userNetId) {
        NetId = netId;

        UserNetId = userNetId;
    }

    public Guid NetId { get; set; }

    public Guid UserNetId { get; set; }
}