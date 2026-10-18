using System.Collections.Generic;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.Shipments;

namespace GBA.Domain.Entities.Transporters;

public sealed class Transporter : EntityBase {
    public Transporter() {
        Sales = new HashSet<Sale>();

        ShipmentLists = new HashSet<ShipmentList>();
    }

    public string Name { get; set; }

    public string CssClass { get; set; }

    public int Priority { get; set; }

    public string? ImageUrl { get; set; }

    public long? TransporterTypeId { get; set; }

    public TransporterType TransporterType { get; set; }

    public ICollection<Sale> Sales { get; set; }

    public ICollection<ShipmentList> ShipmentLists { get; set; }
}