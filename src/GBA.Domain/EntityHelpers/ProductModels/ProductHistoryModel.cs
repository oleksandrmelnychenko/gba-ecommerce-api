using System;

namespace GBA.Domain.EntityHelpers.ProductModels;

public sealed class ProductHistoryModel {
    public ProductHistoryModel(
        long productId,
        Guid productNetId,
        string name,
        string vendorCode,
        double qty,
        DateTime lastOrderedDate) {
        ProductId = productId;
        ProductNetId = productNetId;
        Name = name;
        VendorCode = vendorCode;
        Qty = qty;
        LastOrderedDate = lastOrderedDate;
    }

    public long ProductId { get; set; }
    public Guid ProductNetId { get; set; }
    public string Name { get; set; }
    public string VendorCode { get; set; }
    public double Qty { get; set; }
    public DateTime LastOrderedDate { get; set; }
}