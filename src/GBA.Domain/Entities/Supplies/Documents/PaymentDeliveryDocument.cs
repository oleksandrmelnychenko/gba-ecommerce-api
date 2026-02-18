using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class PaymentDeliveryDocument : BaseDocument {
    public long SupplyOrderPaymentDeliveryProtocolId { get; set; }

    public SupplyOrderPaymentDeliveryProtocol SupplyOrderPaymentDeliveryProtocol { get; set; }
}