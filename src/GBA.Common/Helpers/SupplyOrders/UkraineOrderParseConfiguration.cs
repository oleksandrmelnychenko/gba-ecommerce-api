namespace GBA.Common.Helpers.SupplyOrders;

public sealed class UkraineOrderParseConfiguration {
    public bool WithTotalAmount { get; set; }

    public int VendorCodeColumnNumber { get; set; }

    public int TotalAmountColumnNumber { get; set; }

    public int UnitPriceColumnNumber { get; set; }

    public int QtyColumnNumber { get; set; }

    public int NetWeightColumnNumber { get; set; }

    public int SupplierColumnNumber { get; set; }

    public int StartRow { get; set; }

    public int EndRow { get; set; }
}