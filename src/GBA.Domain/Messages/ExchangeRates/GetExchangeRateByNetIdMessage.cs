using System;

namespace GBA.Domain.Messages.ExchangeRates;

public sealed class GetExchangeRateByNetIdMessage {
    public GetExchangeRateByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}