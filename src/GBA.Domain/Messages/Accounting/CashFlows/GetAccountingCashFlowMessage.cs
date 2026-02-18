using System;
using GBA.Domain.EntityHelpers.Supplies;

namespace GBA.Domain.Messages.Accounting.CashFlows;

public sealed class GetAccountingCashFlowMessage {
    public GetAccountingCashFlowMessage(Guid netId, DateTime from, DateTime to, TypePaymentTask typePaymentTask) {
        NetId = netId;

        TypePaymentTask = typePaymentTask;

        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public Guid NetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public TypePaymentTask TypePaymentTask { get; }
}