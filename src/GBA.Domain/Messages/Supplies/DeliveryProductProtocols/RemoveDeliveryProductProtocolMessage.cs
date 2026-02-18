using System;

namespace GBA.Domain.Messages.Supplies.DeliveryProductProtocols;

public sealed class RemoveDeliveryProductProtocolMessage {
    public RemoveDeliveryProductProtocolMessage(
        Guid netId,
        Guid userNetId) {
        NetId = netId;
        UserNetId = userNetId;
    }

    public Guid NetId { get; }

    public Guid UserNetId { get; }
}