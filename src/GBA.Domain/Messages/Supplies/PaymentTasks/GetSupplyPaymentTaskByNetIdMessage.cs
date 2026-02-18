using System;

namespace GBA.Domain.Messages.Supplies.PaymentTasks;

public sealed class GetSupplyPaymentTaskByNetIdMessage {
    public GetSupplyPaymentTaskByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}