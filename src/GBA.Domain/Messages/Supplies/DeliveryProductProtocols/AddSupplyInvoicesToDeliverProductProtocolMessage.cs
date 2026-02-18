using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;

namespace GBA.Domain.Messages.Supplies.DeliveryProductProtocols;

public sealed class AddSupplyInvoicesToDeliverProductProtocolMessage {
    public AddSupplyInvoicesToDeliverProductProtocolMessage(
        DeliveryProductProtocol protocol) {
        Protocol = protocol;
    }

    public DeliveryProductProtocol Protocol { get; }
}