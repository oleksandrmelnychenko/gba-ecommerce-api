namespace GBA.Domain.Messages.Pricings;

public sealed class UpdatePricingPriorityMessage {
    public UpdatePricingPriorityMessage(long pricingId, bool raise) {
        PricingId = pricingId;
        Raise = raise;
    }

    public long PricingId { get; }
    public bool Raise { get; }
}