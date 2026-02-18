using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.Messages.Pricings;

public sealed class UpdateProviderPricingMessage {
    public UpdateProviderPricingMessage(ProviderPricing providerPricing) {
        ProviderPricing = providerPricing;
    }

    public ProviderPricing ProviderPricing { get; set; }
}