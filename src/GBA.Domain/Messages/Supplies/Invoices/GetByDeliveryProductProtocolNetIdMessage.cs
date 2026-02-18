using System;

namespace GBA.Domain.Messages.Supplies.Invoices;

public sealed class GetByDeliveryProductProtocolNetIdMessage {
    public GetByDeliveryProductProtocolNetIdMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}