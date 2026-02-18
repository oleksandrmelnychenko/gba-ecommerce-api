using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncSaleItem {
    public string DocumentNumber { get; set; }

    public DateTime DocumentDate { get; set; }

    public bool IsVatSale { get; set; }

    public decimal RateExchange { get; set; }

    public string DocumentResponsibleName { get; set; }

    public long ProductCode { get; set; }

    public double Quantity { get; set; }

    public decimal Price { get; set; }
}