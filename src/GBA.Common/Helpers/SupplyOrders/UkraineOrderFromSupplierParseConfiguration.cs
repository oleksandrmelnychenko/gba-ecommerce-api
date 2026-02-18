namespace GBA.Common.Helpers.SupplyOrders;

public sealed class UkraineOrderFromSupplierParseConfiguration {
    public bool IsPricePerItem { get; set; }

    public bool IsWeightPerItem { get; set; }

    public bool WithWeight { get; set; }

    public bool WithGrossWeight { get; set; }

    public bool WithSpecificationCode { get; set; }

    public int UnitPriceColumnNumber { get; set; }

    public int TotalAmountColumnNumber { get; set; }

    public int QtyColumnNumber { get; set; }

    public int VendorCodeColumnNumber { get; set; }

    public int WeightColumnNumber { get; set; }

    public int GrossWeightColumnNumber { get; set; }

    public int SpecificationCodeColumnNumber { get; set; }

    public int StartRow { get; set; }

    public int EndRow { get; set; }

    public bool WithIsImportedProduct { get; set; }

    public int IsImportedProduct { get; set; }
}