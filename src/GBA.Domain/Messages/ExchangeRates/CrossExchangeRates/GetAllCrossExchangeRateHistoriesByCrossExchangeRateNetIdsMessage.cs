using System;
using System.Collections.Generic;

namespace GBA.Domain.Messages.ExchangeRates;

public sealed class GetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdsMessage {
    public GetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdsMessage(IEnumerable<Guid> crossExchangeRateNetIds, long limit, long offset, DateTime from, DateTime to) {
        CrossExchangeRateNetIds = crossExchangeRateNetIds;

        Limit = limit;

        Offset = offset;

        From = from;

        To = to;
    }

    public IEnumerable<Guid> CrossExchangeRateNetIds { get; set; }

    public long Limit { get; set; }

    public long Offset { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }
}