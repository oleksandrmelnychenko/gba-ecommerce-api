using System;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Messages.Supplies;

public sealed class AddPolandPaymentDeliveryProtocolDocumentsMessage {
    public AddPolandPaymentDeliveryProtocolDocumentsMessage(Guid netId, SupplyOrderPolandPaymentDeliveryProtocol polandPaymentDeliveryProtocol) {
        PolandPaymentDeliveryProtocol = polandPaymentDeliveryProtocol;
        NetId = netId;
    }

    public Guid NetId { get; set; }

    public SupplyOrderPolandPaymentDeliveryProtocol PolandPaymentDeliveryProtocol { get; set; }
}