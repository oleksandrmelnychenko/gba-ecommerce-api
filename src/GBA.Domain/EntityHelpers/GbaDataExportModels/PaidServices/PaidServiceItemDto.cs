namespace GBA.Domain.EntityHelpers.GbaDataExportModels.PaidServices;

public sealed class PaidServiceItemDto {
    public ProductDto Product { get; set; }
    public string MeasureUnit { get; set; }
    public double Qty { get; set; }

    public decimal Amount { get; set; }

    public decimal ExpenseAmount { get; set; }

    public double Weight { get; set; }
    // public decimal VatAmount { get; set; }
}