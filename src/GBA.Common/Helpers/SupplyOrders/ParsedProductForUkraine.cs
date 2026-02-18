namespace GBA.Common.Helpers.SupplyOrders;

public sealed class ParsedProductForUkraine {
    public long ProductId { get; set; }

    public string SupplierName { get; set; }

    public string VendorCode { get; set; }

    public double Qty { get; set; }

    public double TotalNetWeight { get; set; }

    public double TotalGrossWeight { get; set; }

    public string SpecificationCode { get; set; }

    public decimal TotalNetPrice { get; set; }

    public decimal UnitPrice { get; set; }

    public bool ProductIsImported { get; set; }
}