using System;
using System.Collections.Generic;
using GBA.Domain.EntityHelpers.Charts;

namespace GBA.Domain.Repositories.Charts.Contracts;

public interface ICurrencyTraderExchangeRateChartsRepository {
    IEnumerable<ForChartCurrencyTrader> GetCurrencyTraderExchangeRatesFiltered(
        DateTime from,
        DateTime to,
        IEnumerable<Guid> traderNetIds,
        IEnumerable<string> currencies
    );
}