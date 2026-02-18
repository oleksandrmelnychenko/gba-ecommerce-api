using System.Collections.Generic;

namespace GBA.Domain.Entities.Products;

public sealed class ProductProductGroup : EntityBase {
    public long ProductGroupId { get; set; }

    public long ProductId { get; set; }

    public string VendorCode { get; set; }

    public double OrderStandard { get; set; }

    public ProductGroup ProductGroup { get; set; }

    public Product Product { get; set; }

    public List<ProductGroup> ProductGroups { get; set; }
}