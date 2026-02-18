using System;

namespace GBA.Domain.Messages.Currencies.CurrencyTraders;

public sealed class DeleteCurrencyTraderByNetIdMessage {
    public DeleteCurrencyTraderByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}