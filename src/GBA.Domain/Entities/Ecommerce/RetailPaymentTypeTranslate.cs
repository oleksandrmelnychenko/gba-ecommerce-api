namespace GBA.Domain.Entities.Ecommerce;

public sealed class RetailPaymentTypeTranslate : EntityBase {
    public string LowPrice { get; set; }

    public string FullPrice { get; set; }

    public string CultureCode { get; set; }

    public string Comment { get; set; }

    public string FastOrderSuccessMessage { get; set; }

    public string ScreenshotMessage { get; set; }
}