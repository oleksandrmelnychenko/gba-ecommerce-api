using System;

namespace GBA.Domain.Messages.Consumables.Orders;

public sealed class GetAllConsumablesOrdersMessage {
    public GetAllConsumablesOrdersMessage(DateTime from, DateTime to) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public DateTime From { get; }

    public DateTime To { get; }
}