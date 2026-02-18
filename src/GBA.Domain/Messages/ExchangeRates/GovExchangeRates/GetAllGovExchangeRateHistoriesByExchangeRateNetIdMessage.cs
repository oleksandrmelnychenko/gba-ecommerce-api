using System;

namespace GBA.Domain.Messages.ExchangeRates.GovExchangeRates;

public sealed class GetAllGovExchangeRateHistoriesByExchangeRateNetIdMessage {
    public GetAllGovExchangeRateHistoriesByExchangeRateNetIdMessage(Guid exchangeRateNetId, long limit, long offset) {
        ExchangeRateNetId = exchangeRateNetId;

        Limit = limit;

        Offset = offset;
    }

    public Guid ExchangeRateNetId { get; set; }

    public long Limit { get; set; }

    public long Offset { get; set; }
}