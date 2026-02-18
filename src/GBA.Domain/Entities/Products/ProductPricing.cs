using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.Entities.Products;

public sealed class ProductPricing : EntityBase {
    public long PricingId { get; set; }

    public long ProductId { get; set; }

    public decimal Price { get; set; }

    public Pricing Pricing { get; set; }

    public Product Product { get; set; }
}