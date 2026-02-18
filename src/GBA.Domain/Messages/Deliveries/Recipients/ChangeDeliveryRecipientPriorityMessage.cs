namespace GBA.Domain.Messages.Deliveries.Recipients;

public sealed class ChangeDeliveryRecipientPriorityMessage {
    public ChangeDeliveryRecipientPriorityMessage(long increaseTo, long? decreaseTo = null) {
        IncreaseTo = increaseTo;

        DecreaseTo = decreaseTo;
    }

    public long IncreaseTo { get; set; }

    public long? DecreaseTo { get; set; }
}