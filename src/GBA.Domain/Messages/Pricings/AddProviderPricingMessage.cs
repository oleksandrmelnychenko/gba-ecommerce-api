using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.Messages.Pricings;

public sealed class AddProviderPricingMessage {
    public AddProviderPricingMessage(ProviderPricing providerPricing) {
        ProviderPricing = providerPricing;
    }

    public ProviderPricing ProviderPricing { get; set; }
}