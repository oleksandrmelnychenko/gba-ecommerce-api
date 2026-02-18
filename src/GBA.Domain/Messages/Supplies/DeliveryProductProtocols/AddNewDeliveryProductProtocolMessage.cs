using System;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;

namespace GBA.Domain.Messages.Supplies.DeliveryProductProtocols;

public sealed class AddNewDeliveryProductProtocolMessage {
    public AddNewDeliveryProductProtocolMessage(
        DeliveryProductProtocol deliveryProductProtocol,
        Guid userNtUId) {
        DeliveryProductProtocol = deliveryProductProtocol;

        UserNtUId = userNtUId;
    }

    public DeliveryProductProtocol DeliveryProductProtocol { get; }

    public Guid UserNtUId { get; }
}