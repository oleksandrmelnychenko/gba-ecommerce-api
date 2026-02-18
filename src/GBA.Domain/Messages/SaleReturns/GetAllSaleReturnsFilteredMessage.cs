using System;

namespace GBA.Domain.Messages.SaleReturns;

public sealed class GetAllSaleReturnsFilteredMessage {
    public GetAllSaleReturnsFilteredMessage(DateTime from, DateTime to, long limit, long offset, string value) {
        From = from.Year.Equals(1) ? DateTime.Now.Date : from;

        To = to.Year.Equals(1) ? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.AddHours(23).AddMinutes(59).AddSeconds(59);

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