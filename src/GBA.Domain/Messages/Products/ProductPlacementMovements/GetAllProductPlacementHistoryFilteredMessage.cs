using System;

namespace GBA.Domain.Messages.Products.ProductPlacementMovements;

public sealed class GetAllProductPlacementHistoryFilteredMessage {
    public GetAllProductPlacementHistoryFilteredMessage(
        Guid productNetId,
        DateTime from,
        DateTime to,
        long limit,
        long offset
    ) {
        ProductNetId = productNetId;

        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public Guid ProductNetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public long Limit { get; }

    public long Offset { get; }
}