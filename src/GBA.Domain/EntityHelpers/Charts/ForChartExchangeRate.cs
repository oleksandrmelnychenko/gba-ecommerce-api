using System.Collections.Generic;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.EntityHelpers.Charts;

public sealed class ForChartExchangeRate {
    public ForChartExchangeRate(ExchangeRate exchangeRate) {
        ExchangeRate = exchangeRate;

        ExchangeRateValues = new List<ForChartExchangeRateValue>();
    }

    public ForChartExchangeRate(CrossExchangeRate crossExchangeRate) {
        CrossExchangeRate = crossExchangeRate;

        ExchangeRateValues = new List<ForChartExchangeRateValue>();
    }

    public ExchangeRate ExchangeRate { get; set; }

    public CrossExchangeRate CrossExchangeRate { get; set; }

    public List<ForChartExchangeRateValue> ExchangeRateValues { get; set; }
}