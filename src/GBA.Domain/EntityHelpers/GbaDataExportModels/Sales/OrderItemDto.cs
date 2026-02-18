namespace GBA.Domain.EntityHelpers.GbaDataExportModels.Sales;

public class OrderItemDto {
    public ProductDto Product { get; set; }
    public double Qty { get; set; }
    public string MeasureUnit { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Discount { get; set; }
    public string StorageName { get; set; }
}

public class ExtendedOrderItemDto : OrderItemDto {
    public SaleDto Sale { get; set; }
}