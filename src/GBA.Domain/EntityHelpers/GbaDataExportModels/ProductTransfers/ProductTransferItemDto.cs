namespace GBA.Domain.EntityHelpers.GbaDataExportModels.ProductTransfers;

public sealed class ProductTransferItemDto {
    public double Qty { get; set; }

    public string Reason { get; set; }

    public ProductDto Product { get; set; }
}