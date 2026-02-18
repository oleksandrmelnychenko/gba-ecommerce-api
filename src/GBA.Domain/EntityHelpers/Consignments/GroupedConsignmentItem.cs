using System;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers.Consignments;

public sealed class GroupedConsignmentItem {
    public decimal NetPrice { get; set; }

    public decimal GrossPrice { get; set; }

    public decimal AccountingGrossPrice { get; set; }

    public double Weight { get; set; }

    public double RemainingQty { get; set; }

    public DateTime FromDate { get; set; }

    public Product Product { get; set; }
}