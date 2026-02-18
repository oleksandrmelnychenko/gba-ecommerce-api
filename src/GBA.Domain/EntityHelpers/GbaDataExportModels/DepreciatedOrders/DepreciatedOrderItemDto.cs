namespace GBA.Domain.EntityHelpers.GbaDataExportModels.DepreciatedOrders;

public sealed class DepreciatedOrderItemDto {
    public decimal Amount { get; set; }

    public decimal Price { get; set; }

    public string MeasureUnit { get; set; }

    public double Qty { get; set; }

    public string Reason { get; set; }

    public ProductDto Product { get; set; }
}