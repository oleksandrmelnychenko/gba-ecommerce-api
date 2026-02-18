using GBA.Common.Helpers.SupplyOrders;

namespace GBA.Domain.EntityHelpers.Supplies;

public sealed class UkraineOrderValidationItem {
    public bool HasError { get; set; }

    public bool VendorCodeNotFinded { get; set; }

    public bool ZeroAvailable { get; set; }

    public bool LessAvailable { get; set; }

    public double AvailableQty { get; set; }

    public string ProductVendorCode { get; set; }

    public ParsedProductForUkraine ParsedProduct { get; set; }
}