namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncProductPrice {
    public string PricingName { get; set; }

    public long ProductCode { get; set; }

    public decimal Price { get; set; }
}