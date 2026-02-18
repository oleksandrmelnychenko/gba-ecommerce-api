using System;

namespace GBA.Domain.Messages.Deliveries.Recipients;

public sealed class ReturnRemoveNetIdMessage {
    public ReturnRemoveNetIdMessage(Guid deliveryRecipientNetId) {
        DeliveryRecipientNetId = deliveryRecipientNetId;
    }

    public Guid DeliveryRecipientNetId { get; set; }
}