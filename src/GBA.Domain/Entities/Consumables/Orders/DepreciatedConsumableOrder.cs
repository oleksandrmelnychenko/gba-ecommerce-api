using System.Collections.Generic;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Entities.Consumables;

public sealed class DepreciatedConsumableOrder : EntityBase {
    public DepreciatedConsumableOrder() {
        DepreciatedConsumableOrderItems = new HashSet<DepreciatedConsumableOrderItem>();

        PriceTotals = new List<PriceTotal>();
    }

    public string Number { get; set; }

    public string Comment { get; set; }

    public long CreatedById { get; set; }

    public long? UpdatedById { get; set; }

    public long DepreciatedToId { get; set; }

    public long CommissionHeadId { get; set; }

    public long ConsumablesStorageId { get; set; }

    public User CreatedBy { get; set; }

    public User UpdatedBy { get; set; }

    public User DepreciatedTo { get; set; }

    public User CommissionHead { get; set; }

    public ConsumablesStorage ConsumablesStorage { get; set; }

    public ICollection<DepreciatedConsumableOrderItem> DepreciatedConsumableOrderItems { get; set; }

    public List<PriceTotal> PriceTotals { get; set; }
}