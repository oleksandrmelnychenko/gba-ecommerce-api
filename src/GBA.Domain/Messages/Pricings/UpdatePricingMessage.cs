using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.Messages.Pricings;

public sealed class UpdatePricingMessage {
    public UpdatePricingMessage(Pricing pricing) {
        Pricing = pricing;
    }

    public Pricing Pricing { get; set; }
}