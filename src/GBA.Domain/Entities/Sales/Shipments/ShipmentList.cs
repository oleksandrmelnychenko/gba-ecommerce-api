using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Transporters;

namespace GBA.Domain.Entities.Sales.Shipments;

public sealed class ShipmentList : EntityBase {
    public ShipmentList() {
        ShipmentListItems = new HashSet<ShipmentListItem>();
    }

    public string Number { get; set; }

    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public bool IsSent { get; set; }

    public long TransporterId { get; set; }

    public long ResponsibleId { get; set; }

    public Transporter Transporter { get; set; }

    public User Responsible { get; set; }

    public ICollection<ShipmentListItem> ShipmentListItems { get; set; }
}