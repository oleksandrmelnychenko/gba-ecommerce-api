using System;

namespace GBA.Domain.Messages.Vats;

public sealed class GetVatInfoFilteredMessage {
    public GetVatInfoFilteredMessage(DateTime from, DateTime to, int limit, int offset) {
        From = from.Year.Equals(1) ? DateTime.Now.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public int Limit { get; }

    public int Offset { get; }
}