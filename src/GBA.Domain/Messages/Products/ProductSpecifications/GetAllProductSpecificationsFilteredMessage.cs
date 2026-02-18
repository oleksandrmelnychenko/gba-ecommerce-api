namespace GBA.Domain.Messages.Products.ProductSpecifications;

public sealed class GetAllProductSpecificationsFilteredMessage {
    public GetAllProductSpecificationsFilteredMessage(
        string vendorCode,
        string specificationCode,
        string locale,
        long limit,
        long offset) {
        VendorCode = vendorCode;
        SpecificationCode = specificationCode;
        Locale = locale;
        Limit = limit;
        Offset = offset;
    }

    public string VendorCode { get; }
    public string SpecificationCode { get; }
    public string Locale { get; }
    public long Limit { get; }
    public long Offset { get; }
}