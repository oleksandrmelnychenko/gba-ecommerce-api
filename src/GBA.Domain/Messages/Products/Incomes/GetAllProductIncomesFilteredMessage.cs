using System;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class GetAllProductIncomesFilteredMessage {
    public GetAllProductIncomesFilteredMessage(DateTime from, DateTime to, long limit, long offset, string value) {
        DateTime FromOnly = new(from.Year, from.Month, from.Day);
        DateTime ToOnly = new(to.Year, to.Month, to.Day);

        From = FromOnly.Year.Equals(1) ? DateTime.UtcNow.Date : FromOnly.Date;
        To = ToOnly.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : ToOnly.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;

        Value = string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public long Limit { get; }

    public long Offset { get; }

    public string Value { get; }
}