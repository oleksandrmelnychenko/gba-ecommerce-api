using System;
using System.Collections.Generic;
using System.Linq;

namespace GBA.Domain.Messages.Charts.CurrencyTraderExchangeRatesCharts;

public sealed class GetCurrencyTraderExchangeRatesFilteredMessage {
    public GetCurrencyTraderExchangeRatesFilteredMessage(
        DateTime from,
        DateTime to,
        IEnumerable<Guid> traderNetIds,
        IEnumerable<string> currencies
    ) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddDays(7) : to.Date.AddDays(7);

        TraderNetIds = traderNetIds;

        Currencies = currencies.Where(c => !string.IsNullOrEmpty(c));
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public IEnumerable<Guid> TraderNetIds { get; }

    public IEnumerable<string> Currencies { get; }
}