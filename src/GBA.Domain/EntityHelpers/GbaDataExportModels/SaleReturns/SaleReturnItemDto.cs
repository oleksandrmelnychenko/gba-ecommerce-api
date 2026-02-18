using GBA.Domain.EntityHelpers.GbaDataExportModels.Sales;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.SaleReturns;

public sealed class SaleReturnItemDto {
    public double Qty { get; set; }

    public decimal ExchangeRateAmount { get; set; }
    public decimal Amount { get; set; }

    // public decimal AmountLocal { get; set; }
    public decimal VatAmount { get; set; }

    // public decimal VatAmountLocal { get; set; }
    public string StorageName { get; set; }


    // sale order item fields

    public ProductDto Product { get; set; }
    public double OrderItemQty { get; set; }
    public string MeasureUnit { get; set; }
    public decimal Price { get; set; }
    public decimal OrderItemAmount { get; set; }
    public decimal OrderItemVatAmount { get; set; }
    public decimal Discount { get; set; }
    public string OrderItemStorageName { get; set; }

    public SaleDto Sale { get; set; }
    // public ExtendedOrderItemDto SaleOrderItem { get; set; }
}