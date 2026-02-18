using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Entities.Supplies.Returns;

public sealed class SupplyReturn : EntityBase {
    public SupplyReturn() {
        SupplyReturnItems = new HashSet<SupplyReturnItem>();
    }

    public string Number { get; set; }

    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public long SupplierId { get; set; }

    public long ClientAgreementId { get; set; }

    public long OrganizationId { get; set; }

    public long ResponsibleId { get; set; }

    public long StorageId { get; set; }

    public decimal TotalNetPrice { get; set; }

    public double TotalNetWeight { get; set; }

    public double TotalQty { get; set; }

    public bool IsManagement { get; set; }

    public Client Supplier { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public Organization Organization { get; set; }

    public User Responsible { get; set; }

    public Storage Storage { get; set; }

    public ICollection<SupplyReturnItem> SupplyReturnItems { get; set; }
}