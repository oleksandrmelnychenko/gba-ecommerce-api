using GBA.Domain.Entities;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.ReSales;

public sealed class ReSaleAvailabilityDto {
    public ProductDto Product { get; set; }

    public string SpecificationCode { get; set; }

    public Storage FromStorage { get; set; }

    public string ProductGroup { get; set; }

    public double Qty { get; set; }

    public string MeasureUnit { get; set; }

    public decimal AccountingGrossPrice { get; set; }

    public decimal SalePrice { get; set; }

    public decimal TotalAccountingPrice { get; set; }

    public decimal TotalSalePrice { get; set; }

    public double Weight { get; set; }

    public decimal ExchangeRate { get; set; }
}