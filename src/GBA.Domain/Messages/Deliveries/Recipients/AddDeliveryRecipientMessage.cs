using GBA.Domain.Entities.Delivery;

namespace GBA.Domain.Messages.Deliveries.Recipients;

public sealed class AddDeliveryRecipientMessage {
    public AddDeliveryRecipientMessage(DeliveryRecipient deliveryRecipient) {
        DeliveryRecipient = deliveryRecipient;
    }

    public DeliveryRecipient DeliveryRecipient { get; set; }
}