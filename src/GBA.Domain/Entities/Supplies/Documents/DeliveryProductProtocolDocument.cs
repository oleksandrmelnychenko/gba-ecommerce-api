using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;

namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class DeliveryProductProtocolDocument : BaseDocument {
    public string Number { get; set; }

    public long DeliveryProductProtocolId { get; set; }

    public DeliveryProductProtocol DeliveryProductProtocol { get; set; }
}