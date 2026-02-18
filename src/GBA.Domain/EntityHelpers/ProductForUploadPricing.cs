namespace GBA.Domain.EntityHelpers;

public sealed class ProductForUploadPricing {
    public string Name { get; set; }

    public long PricingId { get; set; }

    public decimal Price { get; set; }
}