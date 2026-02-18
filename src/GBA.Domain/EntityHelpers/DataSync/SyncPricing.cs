namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncPricing {
    public string Name { get; set; }

    public string BaseName { get; set; }

    public decimal Discount { get; set; }

    public bool ForVat { get; set; }
}