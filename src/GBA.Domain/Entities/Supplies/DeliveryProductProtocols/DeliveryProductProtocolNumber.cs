namespace GBA.Domain.Entities.Supplies.DeliveryProductProtocols;

public sealed class DeliveryProductProtocolNumber : EntityBase {
    public string Number { get; set; }

    public DeliveryProductProtocol DeliveryProductProtocol { get; set; }
}