using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;

public sealed class AddNewIncomePaymentOrderWithoutAgreementMessage {
    public AddNewIncomePaymentOrderWithoutAgreementMessage(IncomePaymentOrder incomePaymentOrder, Guid userNetId) {
        IncomePaymentOrder = incomePaymentOrder;

        UserNetId = userNetId;
    }

    public IncomePaymentOrder IncomePaymentOrder { get; set; }

    public Guid UserNetId { get; set; }
}