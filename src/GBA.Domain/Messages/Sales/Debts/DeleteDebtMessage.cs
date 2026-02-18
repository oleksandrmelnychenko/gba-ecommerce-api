using System;

namespace GBA.Domain.Messages.Sales.Debts;

public sealed class DeleteDebtMessage {
    public DeleteDebtMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}