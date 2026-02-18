using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetAllSalesFromPlUkGroupedByClientFilteredMessage {
    public GetAllSalesFromPlUkGroupedByClientFilteredMessage(
        DateTime from,
        DateTime to,
        string value
    ) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Value = string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public string Value { get; }
}