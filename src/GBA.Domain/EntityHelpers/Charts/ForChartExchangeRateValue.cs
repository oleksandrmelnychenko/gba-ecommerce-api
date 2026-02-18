using System;

namespace GBA.Domain.EntityHelpers.Charts;

public sealed class ForChartExchangeRateValue {
    public DateTime Created { get; set; }

    public decimal Value { get; set; }
}