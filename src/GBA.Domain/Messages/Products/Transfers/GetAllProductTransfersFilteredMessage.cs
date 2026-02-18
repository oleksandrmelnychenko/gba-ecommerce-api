using System;

namespace GBA.Domain.Messages.Products.Transfers;

public sealed class GetAllProductTransfersFilteredMessage {
    public GetAllProductTransfersFilteredMessage(
        DateTime from,
        DateTime to,
        long limit,
        long offset
    ) {
        From = from.Year.Equals(1) ? DateTime.Now.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public long Limit { get; }

    public long Offset { get; }
}