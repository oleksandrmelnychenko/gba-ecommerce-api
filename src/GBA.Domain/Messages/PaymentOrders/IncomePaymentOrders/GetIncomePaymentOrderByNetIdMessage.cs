using System;

namespace GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;

public sealed class GetIncomePaymentOrderByNetIdMessage {
    public GetIncomePaymentOrderByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}