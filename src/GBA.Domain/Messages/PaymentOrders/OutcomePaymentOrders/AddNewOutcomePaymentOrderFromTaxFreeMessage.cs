using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;

public sealed class AddNewOutcomePaymentOrderFromTaxFreeMessage {
    public AddNewOutcomePaymentOrderFromTaxFreeMessage(OutcomePaymentOrder outcomePaymentOrder, Guid taxFreeNetId, Guid userNetId) {
        OutcomePaymentOrder = outcomePaymentOrder;

        TaxFreeNetId = taxFreeNetId;

        UserNetId = userNetId;
    }

    public OutcomePaymentOrder OutcomePaymentOrder { get; }
    public Guid TaxFreeNetId { get; }
    public Guid UserNetId { get; }
}