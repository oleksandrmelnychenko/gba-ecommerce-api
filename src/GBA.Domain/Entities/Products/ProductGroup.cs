using System.Collections.Generic;
using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.Entities.Products;

public sealed class ProductGroup : EntityBase {
    public ProductGroup() {
        ProductGroupDiscounts = new HashSet<ProductGroupDiscount>();

        RootProductGroups = new HashSet<ProductSubGroup>();

        SubProductGroups = new HashSet<ProductSubGroup>();

        ProductProductGroups = new HashSet<ProductProductGroup>();

        PricingProductGroupDiscounts = new HashSet<PricingProductGroupDiscount>();

        ProductWriteOffRules = new HashSet<ProductWriteOffRule>();
    }

    public string Name { get; set; }

    public string FullName { get; set; }

    public string Description { get; set; }

    public bool IsSubGroup { get; set; }

    public byte[] SourceAmgId { get; set; }

    public byte[] SourceFenixId { get; set; }

    public bool IsActive { get; set; }

    public ICollection<ProductGroupDiscount> ProductGroupDiscounts { get; set; }

    public ICollection<ProductSubGroup> RootProductGroups { get; set; }

    public ICollection<ProductSubGroup> SubProductGroups { get; set; }

    public ICollection<ProductProductGroup> ProductProductGroups { get; set; }

    public ICollection<PricingProductGroupDiscount> PricingProductGroupDiscounts { get; set; }

    public ICollection<ProductWriteOffRule> ProductWriteOffRules { get; set; }

    public int TotalProductSubGroup { get; set; }

    public int TotalProduct { get; set; }

    public ProductGroup RootProductGroup { get; set; }
}