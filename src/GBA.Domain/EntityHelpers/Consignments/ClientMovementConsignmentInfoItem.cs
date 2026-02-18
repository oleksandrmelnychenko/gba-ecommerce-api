using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers.Consignments;

public sealed class ClientMovementConsignmentInfoItem {
    public Product Product { get; set; }

    public double ItemQty { get; set; }

    public decimal PricePerItem { get; set; }

    public decimal TotalAmount { get; set; }

    public string ProductSpecificationCode { get; set; }

    public string Responsible { get; set; }
}