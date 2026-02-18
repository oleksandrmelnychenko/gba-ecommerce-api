namespace GBA.Domain.EntityHelpers.GbaDataExportModels.ProductCapitalizations;

public sealed class ProductCapitalizationItemDto {
    public double Qty { get; set; }

    public double RemainingQty { get; set; }

    public double Weight { get; set; }

    public double TotalNetWeight { get; set; }

    public decimal Price { get; set; }

    public decimal Amount { get; set; }

    public long ProductId { get; set; }

    public ProductDto Product { get; set; }
}