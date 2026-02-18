namespace GBA.Common.Helpers.SupplyOrders;

public sealed class CartItemsParseConfiguration {
    public int VendorCodeColumnNumber { get; set; }

    public int QtyColumnNumber { get; set; }

    public int FromDateColumnNumber { get; set; }

    public int PriorityColumnNumber { get; set; }

    public int StartRow { get; set; }

    public int EndRow { get; set; }
}