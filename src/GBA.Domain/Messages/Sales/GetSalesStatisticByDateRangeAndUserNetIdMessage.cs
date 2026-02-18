using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSalesStatisticByDateRangeAndUserNetIdMessage {
    public GetSalesStatisticByDateRangeAndUserNetIdMessage(Guid userNetId, DateTime? from, DateTime? to) {
        UserNetId = userNetId;
        From = from;
        To = to;
    }

    public Guid UserNetId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
}