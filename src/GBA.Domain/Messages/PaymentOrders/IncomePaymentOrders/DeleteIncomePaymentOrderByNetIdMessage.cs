using System;

namespace GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;

public sealed class DeleteIncomePaymentOrderByNetIdMessage {
    public DeleteIncomePaymentOrderByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}