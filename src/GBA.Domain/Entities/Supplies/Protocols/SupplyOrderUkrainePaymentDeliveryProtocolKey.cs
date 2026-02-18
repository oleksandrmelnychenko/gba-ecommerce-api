using System.Collections.Generic;

namespace GBA.Domain.Entities.Supplies.Protocols;

public sealed class SupplyOrderUkrainePaymentDeliveryProtocolKey : EntityBase {
    public SupplyOrderUkrainePaymentDeliveryProtocolKey() {
        SupplyOrderUkrainePaymentDeliveryProtocols = new HashSet<SupplyOrderUkrainePaymentDeliveryProtocol>();
    }

    public string Key { get; set; }

    public ICollection<SupplyOrderUkrainePaymentDeliveryProtocol> SupplyOrderUkrainePaymentDeliveryProtocols { get; set; }
}