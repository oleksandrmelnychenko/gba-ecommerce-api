using System;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.EntityHelpers;

public sealed class ProductSupplyInfo {
    public SupplyInvoice SupplyInvoice { get; set; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public Client Supplier { get; set; }

    public DateTime FromDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public double TotalGrossWeight { get; set; }
}