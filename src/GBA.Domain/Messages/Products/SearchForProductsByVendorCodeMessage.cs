namespace GBA.Domain.Messages.Products;

public sealed class SearchForProductsByVendorCodeMessage {
    public SearchForProductsByVendorCodeMessage(string value, long limit, long offset) {
        Value = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();

        Limit = limit <= 0 ? 44 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public string Value { get; }

    public long Limit { get; }

    public long Offset { get; }
}