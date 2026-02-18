namespace GBA.Domain.EntityHelpers;

public sealed class PackingListItemWithVendorCode {
    public double Qty { get; set; }

    public double NetWeight { get; set; }

    public double GrossWeight { get; set; }

    public decimal UnitPrice { get; set; }

    public string VendorCode { get; set; }
}