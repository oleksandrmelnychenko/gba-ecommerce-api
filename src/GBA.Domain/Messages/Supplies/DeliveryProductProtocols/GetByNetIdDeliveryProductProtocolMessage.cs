using System;

namespace GBA.Domain.Messages.Supplies.DeliveryProductProtocols;

public sealed class GetByNetIdDeliveryProductProtocolMessage {
    public GetByNetIdDeliveryProductProtocolMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}