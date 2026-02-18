using System;

namespace GBA.Domain.Messages.ExchangeRates.GovExchangeRates;

public sealed class GetGovExchangeRateByNetIdMessage {
    public GetGovExchangeRateByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}