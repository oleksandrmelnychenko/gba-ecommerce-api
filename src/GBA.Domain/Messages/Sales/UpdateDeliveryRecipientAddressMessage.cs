using System;
using GBA.Domain.Entities.Delivery;

namespace GBA.Domain.Messages.Sales;

public sealed class UpdateDeliveryRecipientAddressMessage {
    public UpdateDeliveryRecipientAddressMessage(DeliveryRecipientAddress deliveryRecipientAddress, Guid netId) {
        DeliveryRecipientAddress = deliveryRecipientAddress;

        NetId = netId;
    }

    public DeliveryRecipientAddress DeliveryRecipientAddress { get; }

    public Guid NetId { get; }
}