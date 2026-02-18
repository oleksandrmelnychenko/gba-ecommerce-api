using System;

namespace GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;

public sealed class GetAllOutcomePaymentOrdersByColleagueNetIdMessage {
    public GetAllOutcomePaymentOrdersByColleagueNetIdMessage(Guid colleagueNetId) {
        ColleagueNetId = colleagueNetId;
    }

    public Guid ColleagueNetId { get; set; }
}