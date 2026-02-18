namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class ReSaleAvailabilityUsedQty {
    public long ProductId { get; set; }

    public long FromStorageId { get; set; }

    public double Qty { get; set; }
}