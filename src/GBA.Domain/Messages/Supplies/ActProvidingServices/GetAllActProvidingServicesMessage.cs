using System;

namespace GBA.Domain.Messages.Supplies.ActProvidingServices;

public sealed class GetAllActProvidingServicesMessage {
    public GetAllActProvidingServicesMessage(
        DateTime from,
        DateTime to,
        int limit,
        int offset) {
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
        Limit = limit;
        Offset = offset;
    }

    public DateTime From { get; }
    public DateTime To { get; }
    public int Limit { get; }
    public int Offset { get; }
}