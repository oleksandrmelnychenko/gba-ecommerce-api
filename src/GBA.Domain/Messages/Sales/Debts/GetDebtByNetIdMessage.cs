using System;

namespace GBA.Domain.Messages.Sales.Debts;

public sealed class GetDebtByNetIdMessage {
    public GetDebtByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}