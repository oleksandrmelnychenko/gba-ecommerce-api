using System;

namespace GBA.Domain.Messages.Charts.ExchangeRateCharts;

public sealed class GetForUkrainianExchangeRatesRangedMessage {
    public GetForUkrainianExchangeRatesRangedMessage(DateTime from, DateTime to) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddDays(7) : to.Date.AddDays(7);
    }

    public DateTime From { get; }

    public DateTime To { get; }
}