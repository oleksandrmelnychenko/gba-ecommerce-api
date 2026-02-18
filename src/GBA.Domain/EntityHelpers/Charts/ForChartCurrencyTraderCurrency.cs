using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.Charts;

public sealed class ForChartCurrencyTraderCurrency {
    public ForChartCurrencyTraderCurrency() {
        Values = new List<ForChartCurrencyTraderCurrencyValue>();
    }

    public ForChartCurrencyTraderCurrency(string currencyName) {
        CurrencyName = currencyName;

        Values = new List<ForChartCurrencyTraderCurrencyValue>();
    }

    public string CurrencyName { get; set; }

    public List<ForChartCurrencyTraderCurrencyValue> Values { get; set; }
}