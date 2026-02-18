namespace GBA.Domain.EntityHelpers.ProductModels;

public sealed class ProductWithAvailableQty {
    public ProductWithAvailableQty(long productId, string vendorCode, double qty) {
        ProductId = productId;
        VendorCode = vendorCode;
        Qty = qty;
    }

    public long ProductId { get; }
    public string VendorCode { get; }
    public double Qty { get; }
}