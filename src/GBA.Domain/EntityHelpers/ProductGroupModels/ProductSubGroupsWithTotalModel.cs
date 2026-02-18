using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers.ProductGroupModels;

public sealed class ProductSubGroupsWithTotalModel {
    public List<ProductSubGroup> ProductSubGroups { get; set; }

    public int TotalQty { get; set; }

    public int TotalFilteredQty { get; set; }
}