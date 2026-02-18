using System;

namespace GBA.Domain.Messages.Deliveries.Recipients;

public sealed class RemoveNetIdMessage {
    public RemoveNetIdMessage(Guid deliveryRecipientNetId) {
        DeliveryRecipientNetId = deliveryRecipientNetId;
    }

    public Guid DeliveryRecipientNetId { get; set; }
}