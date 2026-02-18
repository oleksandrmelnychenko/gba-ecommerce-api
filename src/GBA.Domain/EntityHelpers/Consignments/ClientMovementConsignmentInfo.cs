using System;
using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.Consignments;

public sealed class ClientMovementConsignmentInfo {
    public ClientMovementConsignmentInfo() {
        InfoItems = new HashSet<ClientMovementConsignmentInfoItem>();
    }

    public long TotalRowsQty { get; set; }

    public long DocumentId { get; set; }

    public string DocumentTypeName { get; set; }

    public string DocumentNumber { get; set; }

    public DateTime DocumentFromDate { get; set; }

    public DateTime DocumentUpdatedDate { get; set; }

    public string OrganizationName { get; set; }

    public decimal TotalEuroAmount { get; set; }

    public double TotalQty { get; set; }

    public double TotalPositions => InfoItems.Count;

    public string Responsible { get; set; }

    public ICollection<ClientMovementConsignmentInfoItem> InfoItems { get; set; }
}