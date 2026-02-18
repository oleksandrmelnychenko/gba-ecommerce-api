namespace GBA.Domain.EntityHelpers.GbaDataExportModels;

public sealed class PricingDto {
    public string Name { get; set; }
    public CurrencyDto Currency { get; set; }
    public string PriceType { get; set; }
    public PricingDto BasePricing { get; set; }
    public double? ExtraCharge { get; set; }
    public bool Vat { get; set; } = true;
    public string KindPriceType { get; set; }
    public string MethodPriceCalculation { get; set; }
}