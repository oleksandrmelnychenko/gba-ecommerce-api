using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Entities.Products;

public sealed class ProductGroupDiscount : EntityBase {
    public ProductGroupDiscount() {
        SubProductGroupDiscounts = new HashSet<ProductGroupDiscount>();
    }

    public long ClientAgreementId { get; set; }

    public long ProductGroupId { get; set; }

    public bool IsActive { get; set; }

    public double DiscountRate { get; set; }

    public ProductGroup ProductGroup { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public ICollection<ProductGroupDiscount> SubProductGroupDiscounts { get; set; }
}