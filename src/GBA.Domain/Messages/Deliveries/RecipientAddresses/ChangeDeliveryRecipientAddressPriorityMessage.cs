namespace GBA.Domain.Messages.Deliveries.RecipientAddresses;

public sealed class ChangeDeliveryRecipientAddressPriorityMessage {
    public ChangeDeliveryRecipientAddressPriorityMessage(long increaseTo, long? decreaseTo = null) {
        IncreaseTo = increaseTo;

        DecreaseTo = decreaseTo;
    }

    public long IncreaseTo { get; set; }

    public long? DecreaseTo { get; set; }
}