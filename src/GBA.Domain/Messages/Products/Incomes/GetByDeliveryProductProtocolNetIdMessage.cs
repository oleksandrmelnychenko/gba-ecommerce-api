using System;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class GetByDeliveryProductProtocolNetIdMessage {
    public GetByDeliveryProductProtocolNetIdMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}