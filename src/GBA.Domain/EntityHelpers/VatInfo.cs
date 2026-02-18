using System;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.EntityHelpers;

public sealed class VatInfo {
    public SupplyInvoice SupplyInvoice { get; set; }

    public Sale Sale { get; set; }

    public DateTime FromDate { get; set; }

    public decimal VatPercent { get; set; } = 23m;

    public decimal VatAmountEU { get; set; }

    public decimal VatAmountPL { get; set; }
}