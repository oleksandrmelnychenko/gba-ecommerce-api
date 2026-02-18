using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class DynamicProductPlacementColumn : EntityBase {
    public DynamicProductPlacementColumn() {
        DynamicProductPlacementRows = new HashSet<DynamicProductPlacementRow>();
    }

    public DateTime FromDate { get; set; }

    public long? PackingListId { get; set; }

    public long? SupplyOrderUkraineId { get; set; }

    public PackingList PackingList { get; set; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public ICollection<DynamicProductPlacementRow> DynamicProductPlacementRows { get; set; }
}