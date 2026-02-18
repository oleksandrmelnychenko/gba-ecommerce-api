namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class ReSaleAvailabilityItemModel {
    public double Qty { get; set; }

    public double QtyToReSale { get; set; }

    public decimal Price { get; set; }

    public decimal Vat { get; set; }

    public decimal SalePrice { get; set; }

    public decimal Amount { get; set; }

    public decimal Profit { get; set; }

    public decimal Profitability { get; set; }

    public long OrganizationId { get; set; }

    public double Weight { get; set; }

    public string VendorCode { get; set; }

    public string ProductName { get; set; }

    public string MeasureUnit { get; set; }

    public string SpecificationCode { get; set; }

    public long ProductId { get; set; }

    public long FromStorageId { get; set; }

    public decimal ExchangeRate { get; set; }

    public ReSaleAvailabilityOldValue OldValue { get; set; }
}