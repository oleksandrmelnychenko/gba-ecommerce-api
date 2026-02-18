using System;

namespace GBA.Domain.Messages.Currencies.CurrencyTraders;

public sealed class GetCurrencyTraderByNetIdMessage {
    public GetCurrencyTraderByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}