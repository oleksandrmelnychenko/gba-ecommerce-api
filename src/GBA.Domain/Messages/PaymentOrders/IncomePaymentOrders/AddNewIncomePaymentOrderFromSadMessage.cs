using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;

public class AddNewIncomePaymentOrderFromSadMessage {
    public AddNewIncomePaymentOrderFromSadMessage(IncomePaymentOrder incomePaymentOrder, Guid sadNetId, Guid userNetId) {
        IncomePaymentOrder = incomePaymentOrder;

        SadNetId = sadNetId;

        UserNetId = userNetId;
    }

    public IncomePaymentOrder IncomePaymentOrder { get; }
    public Guid SadNetId { get; }
    public Guid UserNetId { get; }
}