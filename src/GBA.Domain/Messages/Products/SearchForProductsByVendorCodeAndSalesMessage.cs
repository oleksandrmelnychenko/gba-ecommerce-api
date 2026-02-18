namespace GBA.Domain.Messages.Products;

public sealed class SearchForProductsByVendorCodeAndSalesMessage {
    public SearchForProductsByVendorCodeAndSalesMessage(
        string searchValue,
        long limit,
        long offset
    ) {
        SearchValue = string.IsNullOrEmpty(searchValue) ? string.Empty : searchValue.Trim();

        Limit = limit <= 0 ? 44 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public string SearchValue { get; }

    public long Limit { get; }

    public long Offset { get; }
}