using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;

namespace GBA.Domain.Entities.Consignments;

public sealed class Consignment : EntityBase {
    public Consignment() {
        ConsignmentItems = new HashSet<ConsignmentItem>();
    }

    public bool IsVirtual { get; set; }

    public DateTime FromDate { get; set; }

    public long StorageId { get; set; }

    public long OrganizationId { get; set; }

    public long ProductIncomeId { get; set; }

    public long? ProductTransferId { get; set; }

    public bool IsImportedFromOneC { get; set; }

    public Storage Storage { get; set; }

    public Organization Organization { get; set; }

    public ProductIncome ProductIncome { get; set; }

    public ProductTransfer ProductTransfer { get; set; }

    public ICollection<ConsignmentItem> ConsignmentItems { get; set; }
}