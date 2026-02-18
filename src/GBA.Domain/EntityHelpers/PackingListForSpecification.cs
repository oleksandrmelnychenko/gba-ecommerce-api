using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.EntityHelpers;

public sealed class PackingListForSpecification {
    public PackingListForSpecification() {
        OrderItems = new List<PackingListPackageOrderItem>();
    }

    public string ProductSpecificationCode { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }

    public Client Client { get; set; }

    public Organization Organization { get; set; }

    public List<PackingListPackageOrderItem> OrderItems { get; set; }
}