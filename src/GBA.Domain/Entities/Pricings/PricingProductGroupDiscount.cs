using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities.Pricings;

public sealed class PricingProductGroupDiscount : EntityBase {
    public decimal Amount { get; set; }

    public decimal CalculatedExtraCharge { get; set; }

    public long ProductGroupId { get; set; }

    public long PricingId { get; set; }

    public long? BasePricingId { get; set; }

    public ProductGroup ProductGroup { get; set; }

    public Pricing Pricing { get; set; }

    public Pricing BasePricing { get; set; }
}