using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetAllSalesFilteredByTransporterMessage {
    public GetAllSalesFilteredByTransporterMessage(
        DateTime from,
        DateTime to,
        Guid netId
    ) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        NetId = netId;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public Guid NetId { get; }
}