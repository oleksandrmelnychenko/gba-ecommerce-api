using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;

public class UpdateOutcomePaymentOrderMessage {
    public UpdateOutcomePaymentOrderMessage(OutcomePaymentOrder outcomePaymentOrder, Guid currentUserNetId, bool auto) {
        OutcomePaymentOrder = outcomePaymentOrder;

        CurrentUserNetId = currentUserNetId;

        Auto = auto;
    }

    public OutcomePaymentOrder OutcomePaymentOrder { get; set; }

    public Guid CurrentUserNetId { get; set; }

    public bool Auto { get; set; }
}