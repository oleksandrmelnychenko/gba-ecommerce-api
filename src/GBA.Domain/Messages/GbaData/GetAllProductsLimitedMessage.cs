namespace GBA.Domain.Messages.GbaData;

public sealed class GetAllProductsLimitedMessage {
    public GetAllProductsLimitedMessage(int limit, int offset) {
        Limit = limit;
        Offset = offset;
    }

    public int Limit { get; }
    public int Offset { get; }
}