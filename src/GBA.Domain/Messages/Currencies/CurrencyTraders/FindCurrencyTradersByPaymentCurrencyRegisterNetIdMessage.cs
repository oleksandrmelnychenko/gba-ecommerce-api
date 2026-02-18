using System;

namespace GBA.Domain.Messages.Currencies.CurrencyTraders;

public sealed class FindCurrencyTradersByPaymentCurrencyRegisterNetIdMessage {
    public FindCurrencyTradersByPaymentCurrencyRegisterNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}