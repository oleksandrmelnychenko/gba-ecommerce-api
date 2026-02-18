using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consignments;

namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class GroupingReSaleAvailabilityModel {
    public long ProductId { get; set; }

    public string VendorCode { get; set; }

    public string ProductName { get; set; }

    public string SpecificationCode { get; set; }

    public Storage FromStorage { get; set; }

    public string ProductGroup { get; set; }

    public double Qty { get; set; }

    public string MeasureUnit { get; set; }

    public decimal AccountingGrossPrice { get; set; }

    public decimal SalePrice { get; set; }

    public decimal TotalAccountingPrice { get; set; }

    public decimal TotalSalePrice { get; set; }

    public double Weight { get; set; }

    public decimal ExchangeRate { get; set; }

    public List<ConsignmentItem> ConsignmentItems { get; set; } = new();
}