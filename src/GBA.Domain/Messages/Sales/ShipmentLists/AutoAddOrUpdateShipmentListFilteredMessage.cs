using System;

namespace GBA.Domain.Messages.Sales.ShipmentLists;

public sealed class AutoAddOrUpdateShipmentListFilteredMessage {
    public AutoAddOrUpdateShipmentListFilteredMessage(
        DateTime from,
        DateTime to,
        Guid netId,
        Guid userNetId
    ) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        NetId = netId;

        UserNetId = userNetId;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public Guid NetId { get; }

    public Guid UserNetId { get; }
}