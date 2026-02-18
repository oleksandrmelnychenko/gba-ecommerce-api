using System;

namespace GBA.Domain.Messages.Sales.Offers;

public sealed class GetAllSaleOffersFilteredMessage {
    public GetAllSaleOffersFilteredMessage(DateTime from, DateTime to) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public DateTime From { get; }

    public DateTime To { get; }
}