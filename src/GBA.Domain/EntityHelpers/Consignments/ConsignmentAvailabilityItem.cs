using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers.Consignments;

public sealed class ConsignmentAvailabilityItem {
    public ConsignmentAvailabilityItem() {
        Placements = new List<ProductPlacement>();
    }

    public long Id { get; set; }

    public long ProductId { get; set; }

    public Guid ProductNetId { get; set; }

    public string ProductName { get; set; }

    public string VendorCode { get; set; }

    public double Qty { get; set; }

    public double IncomeQty { get; set; }

    public decimal ExchangeRate { get; set; }

    public DateTime FromDate { get; set; }

    public long StorageId { get; set; }

    public Guid StorageNetId { get; set; }

    public string StorageName { get; set; }

    public decimal NetPrice { get; set; }

    public decimal TotalNetPrice { get; set; }

    public decimal UnitGrossPrice { get; set; }

    public decimal UnitAccountingGrossPrice { get; set; }

    public decimal GrossPrice { get; set; }

    public decimal AccountingGrossPrice { get; set; }

    public List<ProductPlacement> Placements { get; set; }
}