using System.Collections.Generic;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Entities.Consumables;

public sealed class ConsumablesStorage : EntityBase {
    public ConsumablesStorage() {
        ConsumablesOrders = new HashSet<ConsumablesOrder>();

        ConsumableProducts = new HashSet<ConsumableProduct>();

        DepreciatedConsumableOrders = new HashSet<DepreciatedConsumableOrder>();

        PriceTotals = new List<PriceTotal>();
    }

    public string Name { get; set; }

    public string Description { get; set; }

    public long ResponsibleUserId { get; set; }

    public long OrganizationId { get; set; }

    public User ResponsibleUser { get; set; }

    public Organization Organization { get; set; }

    public CompanyCar CompanyCar { get; set; }

    public List<PriceTotal> PriceTotals { get; set; }

    public ICollection<ConsumablesOrder> ConsumablesOrders { get; set; }

    public ICollection<ConsumableProduct> ConsumableProducts { get; set; }

    public ICollection<DepreciatedConsumableOrder> DepreciatedConsumableOrders { get; set; }
}