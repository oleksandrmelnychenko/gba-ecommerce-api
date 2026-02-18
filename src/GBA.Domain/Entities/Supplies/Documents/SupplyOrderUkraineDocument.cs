using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class SupplyOrderUkraineDocument : BaseDocument {
    public long SupplyOrderUkraineId { get; set; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }
}