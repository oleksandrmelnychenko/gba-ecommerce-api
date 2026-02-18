using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class DynamicProductPlacementRow : EntityBase {
    public DynamicProductPlacementRow() {
        DynamicProductPlacements = new HashSet<DynamicProductPlacement>();
    }

    public double Qty { get; set; }

    public long? SupplyOrderUkraineItemId { get; set; }

    public long? PackingListPackageOrderItemId { get; set; }

    public long DynamicProductPlacementColumnId { get; set; }

    public SupplyOrderUkraineItem SupplyOrderUkraineItem { get; set; }

    public PackingListPackageOrderItem PackingListPackageOrderItem { get; set; }

    public DynamicProductPlacementColumn DynamicProductPlacementColumn { get; set; }

    public ICollection<DynamicProductPlacement> DynamicProductPlacements { get; set; }
}