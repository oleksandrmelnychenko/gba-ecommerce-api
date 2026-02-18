using System.Collections.Generic;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.EntityHelpers.Supplies;

public sealed class GroupedProductSpecification {
    public GroupedProductSpecification() {
        SadItems = new List<SadItem>();

        OrderItems = new List<OrderItem>();
    }

    public string SpecificationCode { get; set; }

    public List<SadItem> SadItems { get; set; }

    public List<OrderItem> OrderItems { get; set; }
}