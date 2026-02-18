using GBA.Domain.Entities.Delivery;

namespace GBA.Domain.Messages.Deliveries.RecipientAddresses;

public sealed class AddDeliveryRecipientAddressMessage {
    public AddDeliveryRecipientAddressMessage(DeliveryRecipientAddress deliveryRecipientAddress) {
        DeliveryRecipientAddress = deliveryRecipientAddress;
    }

    public DeliveryRecipientAddress DeliveryRecipientAddress { get; set; }
}