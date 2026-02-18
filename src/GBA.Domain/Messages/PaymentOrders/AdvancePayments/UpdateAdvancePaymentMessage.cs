using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.AdvancePayments;

public sealed class UpdateAdvancePaymentMessage {
    public UpdateAdvancePaymentMessage(AdvancePayment advancePayment, Guid userNetId) {
        AdvancePayment = advancePayment;
        UserNetId = userNetId;
    }

    public AdvancePayment AdvancePayment { get; }
    public Guid UserNetId { get; }
}