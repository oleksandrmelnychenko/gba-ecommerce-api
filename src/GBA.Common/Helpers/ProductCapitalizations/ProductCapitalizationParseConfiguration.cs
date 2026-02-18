namespace GBA.Common.Helpers.ProductCapitalizations;

public sealed class ProductCapitalizationParseConfiguration {
    public int StartRow { get; set; }

    public int EndRow { get; set; }

    public int VendorCodeColumnNumber { get; set; }

    public int QtyColumnNumber { get; set; }

    public int PriceColumnNumber { get; set; }

    public int WeightColumnNumber { get; set; }

    public bool WithPrice { get; set; }

    public bool PricePerItem { get; set; }

    public bool WithWeight { get; set; }

    public bool WeightPerItem { get; set; }
}