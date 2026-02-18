using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.EntityHelpers.Charts;

public sealed class ForChartCurrencyTrader {
    public ForChartCurrencyTrader(CurrencyTrader currencyTrader) {
        CurrencyTrader = currencyTrader;

        Currencies = new List<ForChartCurrencyTraderCurrency>();
    }

    public CurrencyTrader CurrencyTrader { get; set; }

    public List<ForChartCurrencyTraderCurrency> Currencies { get; set; }
}