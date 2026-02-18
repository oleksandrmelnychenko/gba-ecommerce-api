namespace GBA.Common.Helpers.SupplyOrders;

public sealed class DocumentParseConfiguration {
    public int VendorCodeColumnNumber { get; set; }

    public int QtyColumnNumber { get; set; }

    public int NetWeightColumnNumber { get; set; }

    public int GrossWeightColumnNumber { get; set; }

    public int UnitPriceColumnNumber { get; set; }

    public int TotalAmountColumnNumber { get; set; }

    public int StartRow { get; set; }

    public int EndRow { get; set; }

    public bool IsWeightPerUnit { get; set; }

    public bool WithNetWeight { get; set; }

    public bool WithGrossWeight { get; set; }

    public bool WithTotalAmount { get; set; }

    public bool ProductIsImported { get; set; }
}