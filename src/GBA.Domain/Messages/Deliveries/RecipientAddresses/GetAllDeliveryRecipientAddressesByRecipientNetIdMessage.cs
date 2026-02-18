using System;

namespace GBA.Domain.Messages.Deliveries.RecipientAddresses;

public sealed class GetAllDeliveryRecipientAddressesByRecipientNetIdMessage {
    public GetAllDeliveryRecipientAddressesByRecipientNetIdMessage(Guid recipientNetId) {
        RecipientNetId = recipientNetId;
    }

    public Guid RecipientNetId { get; set; }
}