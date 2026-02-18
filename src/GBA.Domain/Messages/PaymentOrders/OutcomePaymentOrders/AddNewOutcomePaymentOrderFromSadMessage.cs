using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;

public class AddNewOutcomePaymentOrderFromSadMessage {
    public AddNewOutcomePaymentOrderFromSadMessage(OutcomePaymentOrder outcomePaymentOrder, Guid sadNetId, Guid userNetId) {
        OutcomePaymentOrder = outcomePaymentOrder;

        SadNetId = sadNetId;

        UserNetId = userNetId;
    }

    public OutcomePaymentOrder OutcomePaymentOrder { get; }
    public Guid SadNetId { get; }
    public Guid UserNetId { get; }
}