using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.AdvancePayments;

public sealed class AddNewAdvancePaymentMessage {
    public AddNewAdvancePaymentMessage(AdvancePayment advancePayment, Guid taxFreeNetId, Guid sadNetId, Guid userNetId) {
        AdvancePayment = advancePayment;
        TaxFreeNetId = taxFreeNetId;
        SadNetId = sadNetId;
        UserNetId = userNetId;
    }

    public AdvancePayment AdvancePayment { get; }
    public Guid TaxFreeNetId { get; }
    public Guid SadNetId { get; }
    public Guid UserNetId { get; }
}