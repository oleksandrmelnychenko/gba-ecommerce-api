namespace GBA.Domain.Messages.Products;

public sealed class GetProductSpecificationCodesFromSearchMessage {
    public GetProductSpecificationCodesFromSearchMessage(string value) {
        Value = string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    public string Value { get; }
}