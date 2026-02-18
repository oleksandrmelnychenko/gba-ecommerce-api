using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;

public sealed class AddNewIncomePaymentOrderMessage {
    public AddNewIncomePaymentOrderMessage(IncomePaymentOrder incomePaymentOrder, bool auto, Guid userNetId) {
        IncomePaymentOrder = incomePaymentOrder;

        Auto = auto;

        UserNetId = userNetId;
    }

    public IncomePaymentOrder IncomePaymentOrder { get; set; }

    public bool Auto { get; set; }

    public Guid UserNetId { get; set; }
}