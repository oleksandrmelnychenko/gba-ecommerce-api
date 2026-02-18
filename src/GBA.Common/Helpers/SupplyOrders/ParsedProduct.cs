using System;

namespace GBA.Common.Helpers.SupplyOrders;

public sealed class ParsedProduct {
    private decimal _unitPrice;

    public string VendorCode { get; set; }

    public decimal UnitPrice {
        get => _unitPrice;
        set => _unitPrice = Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    public double Qty { get; set; }

    public double NetWeight { get; set; }

    public double GrossWeight { get; set; }

    public DateTime FromDate { get; set; }

    public int Priority { get; set; }

    public int RowNumber { get; set; }
}