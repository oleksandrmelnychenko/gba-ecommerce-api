using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.Messages.Pricings;

public sealed class AddPricingMessage {
    public AddPricingMessage(Pricing pricing) {
        Pricing = pricing;
    }

    public Pricing Pricing { get; set; }
}