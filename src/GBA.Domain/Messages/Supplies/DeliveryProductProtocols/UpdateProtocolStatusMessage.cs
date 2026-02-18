using System;

namespace GBA.Domain.Messages.Supplies.DeliveryProductProtocols;

public sealed class UpdateProtocolStatusMessage {
    public UpdateProtocolStatusMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}