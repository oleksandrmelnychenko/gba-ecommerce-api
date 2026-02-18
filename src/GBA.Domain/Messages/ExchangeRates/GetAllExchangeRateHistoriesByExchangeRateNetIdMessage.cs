using System;

namespace GBA.Domain.Messages.ExchangeRates;

public sealed class GetAllExchangeRateHistoriesByExchangeRateNetIdMessage {
    public GetAllExchangeRateHistoriesByExchangeRateNetIdMessage(Guid exchangeRateNetId, long limit, long offset) {
        ExchangeRateNetId = exchangeRateNetId;

        Limit = limit;

        Offset = offset;
    }

    public Guid ExchangeRateNetId { get; set; }

    public long Limit { get; set; }

    public long Offset { get; set; }
}