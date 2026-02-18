using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers.ProductGroupModels;

public sealed class ProductGroupsWithTotalModel {
    public List<ProductGroup> ProductGroups { get; set; }

    public int TotalQty { get; set; }
}