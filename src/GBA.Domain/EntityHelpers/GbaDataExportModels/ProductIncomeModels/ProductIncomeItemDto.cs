namespace GBA.Domain.EntityHelpers.GbaDataExportModels.ProductIncomeModels;

public class ProductIncomeItemDto {
    public ProductDto Product { get; set; }
    public double Qty { get; set; }
    public string MeasureUnit { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public decimal VatPercent { get; set; }

    public decimal VatAmount { get; set; }

    //public decimal TotalNetPrice { get; set; }
    // public string StorageName { get; set; }
    public bool Imported { get; set; }
    public double NetWeight { get; set; }
}