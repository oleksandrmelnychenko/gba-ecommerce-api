using System;

namespace GBA.Domain.Messages.Supplies.Invoices;

public sealed class GetAllSpendingOnServicesByNetIdMessage {
    public GetAllSpendingOnServicesByNetIdMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}