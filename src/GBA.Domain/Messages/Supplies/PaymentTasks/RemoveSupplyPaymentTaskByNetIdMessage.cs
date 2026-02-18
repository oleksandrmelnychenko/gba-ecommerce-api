using System;

namespace GBA.Domain.Messages.Supplies.PaymentTasks;

public sealed class RemoveSupplyPaymentTaskByNetIdMessage {
    public RemoveSupplyPaymentTaskByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}