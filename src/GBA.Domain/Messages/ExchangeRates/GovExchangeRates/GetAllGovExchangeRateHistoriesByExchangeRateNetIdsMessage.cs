using System;
using System.Collections.Generic;

namespace GBA.Domain.Messages.ExchangeRates.GovExchangeRates;

public sealed class GetAllGovExchangeRateHistoriesByExchangeRateNetIdsMessage {
    public GetAllGovExchangeRateHistoriesByExchangeRateNetIdsMessage(IEnumerable<Guid> govExchangeRateNetIds, long limit, long offset, DateTime from, DateTime to) {
        GovExchangeRateNetIds = govExchangeRateNetIds;

        Limit = limit;

        Offset = offset;

        From = from;

        To = to;
    }

    public IEnumerable<Guid> GovExchangeRateNetIds { get; set; }

    public long Limit { get; set; }

    public long Offset { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }
}