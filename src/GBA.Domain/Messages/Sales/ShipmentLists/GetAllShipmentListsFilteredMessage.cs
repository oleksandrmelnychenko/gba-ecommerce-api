using System;

namespace GBA.Domain.Messages.Sales.ShipmentLists;

public sealed class GetAllShipmentListsFilteredMessage {
    public GetAllShipmentListsFilteredMessage(
        DateTime from,
        DateTime to,
        Guid netId,
        long limit,
        long offset
    ) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        NetId = netId;

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public Guid NetId { get; }

    public long Limit { get; }

    public long Offset { get; }
}