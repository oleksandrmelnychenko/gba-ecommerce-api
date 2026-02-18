using System;

namespace GBA.Domain.Messages.Sales.OrderItems;

public sealed class GetEditTransportersMessage {
    public GetEditTransportersMessage(
        DateTime from,
        DateTime to,
        long limit,
        long offset,
        bool isDevelopment) {
        From = from;
        To = to;
        Limit = limit;
        Offset = offset;
        IsDevelopment = isDevelopment;
    }

    public DateTime From { get; }
    public DateTime To { get; }
    public long Limit { get; }
    public long Offset { get; }
    public bool IsDevelopment { get; }
}