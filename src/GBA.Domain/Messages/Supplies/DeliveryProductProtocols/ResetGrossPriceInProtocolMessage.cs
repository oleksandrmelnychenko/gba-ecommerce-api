using System;

namespace GBA.Domain.Messages.Supplies.DeliveryProductProtocols;

public sealed class ResetGrossPriceInProtocolMessage {
    public ResetGrossPriceInProtocolMessage(
        Guid protocolNetId,
        Guid userNetId) {
        ProtocolNetId = protocolNetId;
        UserNetId = userNetId;
    }

    public Guid ProtocolNetId { get; }
    public Guid UserNetId { get; }
}