using System;

namespace GBA.Domain.Messages.ExchangeRates;

public sealed class GetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdMessage {
    public GetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdMessage(Guid netId, long limit, long offset) {
        NetId = netId;

        Limit = limit;

        Offset = offset;
    }

    public Guid NetId { get; set; }

    public long Limit { get; set; }

    public long Offset { get; set; }
}