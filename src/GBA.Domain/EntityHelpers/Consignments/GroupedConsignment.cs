using System;
using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.Consignments;

public sealed class GroupedConsignment {
    public GroupedConsignment() {
        GroupedConsignmentItems = new HashSet<GroupedConsignmentItem>();
    }

    public DateTime FromDate { get; set; }

    public string ProductIncomeNumber { get; set; }

    public string InvoiceNumber { get; set; }

    public string SupplierName { get; set; }

    public string OrganizationName { get; set; }

    public decimal TotalGrossPrice { get; set; }

    public decimal AccountingTotalGrossPrice { get; set; }

    public double TotalWeight { get; set; }

    public int RowNumber { get; set; }

    public ICollection<GroupedConsignmentItem> GroupedConsignmentItems { get; }
}