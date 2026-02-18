using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.EntityHelpers;

public sealed class CalculatedPricingsWithDiscounts {
    public Pricing Pricing { get; set; }

    public decimal RetailPriceEUR { get; set; }

    public decimal RetailPriceLocal { get; set; }

    public decimal PriceEUR { get; set; }

    public decimal? DiscountPriceEUR { get; set; }

    public double? DiscountRate { get; set; }
}