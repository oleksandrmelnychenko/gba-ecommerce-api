using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.Entities.Ecommerce;

public sealed class EcommerceDefaultPricing : EntityBase {
    public long PricingId { get; set; }

    public long PromotionalPricingId { get; set; }

    public Pricing Pricing { get; set; }

    public Pricing PromotionalPricing { get; set; }
}