using System.Collections.Generic;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.EntityHelpers.ExchangeRateModels;

public sealed class GovExchangeRateAndCrossToReturnModel {
    public GovExchangeRate GovExchangeRate { get; set; }

    public List<GovCrossExchangeRate> GovCrossExchangeRates { get; set; }
}