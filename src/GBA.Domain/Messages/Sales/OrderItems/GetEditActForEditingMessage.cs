using System;

namespace GBA.Domain.Messages.Sales.OrderItems;

public sealed class GetEditActForEditingMessage {
    public GetEditActForEditingMessage(
        DateTime from,
        DateTime to,
        long limit,
        long offset,
        bool isDevelopment) {
        DateTime FromOnly = new(from.Year, from.Month, from.Day);
        DateTime ToOnly = new(to.Year, to.Month, to.Day);
        From = FromOnly.Year.Equals(1) ? DateTime.UtcNow.Date : FromOnly.Date;
        To = ToOnly.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : ToOnly.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        //From = from;
        //To = to;
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