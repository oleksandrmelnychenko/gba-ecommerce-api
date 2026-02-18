namespace GBA.Domain.EntityHelpers.DepreciatedOrderModels;

public sealed class ProductMovementItemFromFile {
    public string VendorCode { get; set; }

    public double Qty { get; set; }

    public bool IsError { get; set; }
}