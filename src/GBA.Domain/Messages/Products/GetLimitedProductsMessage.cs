namespace GBA.Domain.Messages.Products;

public sealed class GetLimitedProductsMessage {
    public GetLimitedProductsMessage(long limit, long offset) {
        Limit = limit;
        Offset = offset;
    }

    public long Offset { get; set; }

    public long Limit { get; set; }
}