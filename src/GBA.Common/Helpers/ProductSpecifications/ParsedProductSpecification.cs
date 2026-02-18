using System;

namespace GBA.Common.Helpers;

public sealed class ParsedProductSpecification {
    private decimal _unitPrice;

    public string VendorCode { get; set; }

    public string SpecificationCode { get; set; }

    public decimal CustomsValue { get; set; }

    public decimal Price {
        get => _unitPrice;
        set => _unitPrice = Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    public double Qty { get; set; }

    public decimal Duty { get; set; }

    public decimal VATValue { get; set; }

    public bool HasError { get; set; }

    public int RowNumber { get; set; }

    public int ColumnNumber { get; set; }
}