using System;

namespace GBA.Domain.Messages.ExchangeRates.GovCrossExchangeRates;

public sealed class GetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdMessage {
    public GetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdMessage(Guid netId, long limit, long offset) {
        NetId = netId;

        Limit = limit;

        Offset = offset;
    }

    public Guid NetId { get; }

    public long Limit { get; set; }

    public long Offset { get; set; }
}