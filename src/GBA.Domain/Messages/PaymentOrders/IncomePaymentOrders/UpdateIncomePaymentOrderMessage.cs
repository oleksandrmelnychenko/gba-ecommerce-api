using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;

public sealed class UpdateIncomePaymentOrderMessage {
    public UpdateIncomePaymentOrderMessage(IncomePaymentOrder incomePaymentOrder, Guid userNetid) {
        IncomePaymentOrder = incomePaymentOrder;

        UserNetId = userNetid;
    }

    public IncomePaymentOrder IncomePaymentOrder { get; set; }

    public Guid UserNetId { get; set; }
}