namespace GBA.Domain.EntityHelpers;

public sealed class SupplyOrderTotals {
    public decimal? TotalNetPrice { get; set; }

    public decimal? TotalGrossPrice { get; set; }

    public double? TotalNetWeight { get; set; }

    public double? TotalGrossWeight { get; set; }
}