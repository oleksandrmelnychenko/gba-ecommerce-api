using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;

public sealed class AddNewOutcomePaymentOrderMessage {
    public AddNewOutcomePaymentOrderMessage(OutcomePaymentOrder outcomePaymentOrder, Guid currentuserNetId) {
        OutcomePaymentOrder = outcomePaymentOrder;

        CurrentUserNetId = currentuserNetId;
    }

    public OutcomePaymentOrder OutcomePaymentOrder { get; set; }

    public Guid CurrentUserNetId { get; set; }
}