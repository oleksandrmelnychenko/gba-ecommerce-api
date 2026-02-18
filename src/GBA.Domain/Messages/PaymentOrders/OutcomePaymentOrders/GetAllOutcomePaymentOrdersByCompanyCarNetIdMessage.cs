using System;

namespace GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;

public sealed class GetAllOutcomePaymentOrdersByCompanyCarNetIdMessage {
    public GetAllOutcomePaymentOrdersByCompanyCarNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}