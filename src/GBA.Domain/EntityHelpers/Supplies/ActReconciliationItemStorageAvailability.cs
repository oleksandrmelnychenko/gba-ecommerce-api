using GBA.Domain.Entities;

namespace GBA.Domain.EntityHelpers.Supplies;

public sealed class ActReconciliationItemStorageAvailability {
    public double Qty { get; set; }

    public Storage Storage { get; set; }
}