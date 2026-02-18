using System;
using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.Messages.ReSales;

public sealed class GetAllReSalesMessage {
    public GetAllReSalesMessage(
        DateTime from,
        DateTime to,
        int limit,
        int offset,
        FilterReSaleStatusOption status) {
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
        Limit = limit;
        Offset = offset;
        Status = status;
    }

    public DateTime From { get; }
    public DateTime To { get; }
    public int Limit { get; }
    public int Offset { get; }
    public FilterReSaleStatusOption Status { get; }
}