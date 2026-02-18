using System;
using System.Collections.Generic;
using GBA.Domain.EntityHelpers.Charts;

namespace GBA.Domain.Repositories.Charts.Contracts;

public interface IExchangeRateChartsRepository {
    IEnumerable<ForChartExchangeRate> GetForUkrainianExchangeRatesRanged(DateTime from, DateTime to);

    IEnumerable<ForChartExchangeRate> GetForPolandExchangeRatesRanged(DateTime from, DateTime to);

    IEnumerable<ForChartExchangeRate> GetCrossExchangeRatesRanged(DateTime from, DateTime to);
}