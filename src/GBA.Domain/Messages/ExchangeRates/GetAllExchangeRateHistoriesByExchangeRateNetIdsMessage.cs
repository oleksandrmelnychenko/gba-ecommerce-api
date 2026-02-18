using System;
using System.Collections.Generic;

namespace GBA.Domain.Messages.ExchangeRates;

public sealed class GetAllExchangeRateHistoriesByExchangeRateNetIdsMessage {
    public GetAllExchangeRateHistoriesByExchangeRateNetIdsMessage(IEnumerable<Guid> exchangeRateNetIds, long limit, long offset, DateTime from, DateTime to) {
        ExchangeRateNetIds = exchangeRateNetIds;

        Limit = limit;

        Offset = offset;

        From = from;

        To = to;
    }

    public IEnumerable<Guid> ExchangeRateNetIds { get; set; }

    public long Limit { get; set; }

    public long Offset { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }
}