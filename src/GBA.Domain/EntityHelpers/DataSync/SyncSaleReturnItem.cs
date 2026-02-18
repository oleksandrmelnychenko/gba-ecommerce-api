using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncSaleReturnItem {
    public string DocumentNumber { get; set; }

    public DateTime DocumentDate { get; set; }

    public decimal RateExchange { get; set; }

    public string Comment { get; set; }

    public string ResponsibleName { get; set; }

    public long ProductCode { get; set; }

    public double Quantity { get; set; }

    public decimal Price { get; set; }

    public string SaleNumber { get; set; }

    public string Storage { get; set; }
}