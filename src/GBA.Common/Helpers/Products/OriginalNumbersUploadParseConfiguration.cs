namespace GBA.Common.Helpers.Products;

public sealed class OriginalNumbersUploadParseConfiguration {
    public int From { get; set; }

    public int To { get; set; }

    public int VendorCode { get; set; }

    public int OriginalNumber { get; set; }

    public bool IsCleanBeforeLoading { get; set; }
}