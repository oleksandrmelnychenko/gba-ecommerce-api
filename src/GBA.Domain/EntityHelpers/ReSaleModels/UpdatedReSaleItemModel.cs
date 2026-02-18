using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.ReSales;

namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class UpdatedReSaleItemModel {
    public List<ReSaleItem> ReSaleItems { get; set; } = new();

    public ConsignmentItem ConsignmentItem { get; set; }

    public double Qty { get; set; }

    public decimal Price { get; set; }

    public double QtyToReSale { get; set; }

    public decimal SalePrice { get; set; }

    public decimal Amount { get; set; }

    public decimal Profit { get; set; }

    public decimal Profitability { get; set; }

    public decimal Vat { get; set; }

    public double Weight { get; set; }

    public ReSaleAvailabilityOldValue OldValue { get; set; } = new();

    public decimal TotalAmountLocal { get; set; }

    public decimal TotalAmountEurToUah { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalVat { get; set; }
}