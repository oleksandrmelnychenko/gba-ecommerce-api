using System;
using System.Collections.Generic;

namespace GBA.Domain.Messages.ExchangeRates.GovCrossExchangeRates;

public sealed class GetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdsMessage {
    public GetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdsMessage(
        IEnumerable<Guid> govCrossExchangeRateNetIds,
        long limit,
        long offset,
        DateTime from,
        DateTime to) {
        GovCrossExchangeRateNetIds = govCrossExchangeRateNetIds;

        Limit = limit;

        Offset = offset;

        From = from;

        To = to;
    }

    public IEnumerable<Guid> GovCrossExchangeRateNetIds { get; set; }

    public long Limit { get; set; }

    public long Offset { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }
}