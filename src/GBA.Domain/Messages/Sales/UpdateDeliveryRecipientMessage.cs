using System;
using GBA.Domain.Entities.Delivery;

namespace GBA.Domain.Messages.Sales;

public sealed class UpdateDeliveryRecipientMessage {
    public UpdateDeliveryRecipientMessage(DeliveryRecipient deliveryRecipient, Guid netId) {
        DeliveryRecipient = deliveryRecipient;

        NetId = netId;
    }

    public DeliveryRecipient DeliveryRecipient { get; }

    public Guid NetId { get; }
}