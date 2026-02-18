using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.EntityHelpers;

public sealed class GroupedByClientSales {
    public GroupedByClientSales() {
        Sales = new List<Sale>();
    }

    public Client Client { get; set; }

    public List<Sale> Sales { get; set; }
}