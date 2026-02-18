namespace GBA.Domain.Messages.Sales.PreOrders;

public sealed class GetAllPreOrdersByCurrentCultureFilteredMessage {
    public GetAllPreOrdersByCurrentCultureFilteredMessage(long limit, long offset) {
        Limit = limit <= 0 ? 30 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public long Limit { get; }

    public long Offset { get; }
}