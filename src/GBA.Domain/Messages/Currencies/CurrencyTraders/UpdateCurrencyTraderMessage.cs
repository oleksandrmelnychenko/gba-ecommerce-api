using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Currencies.CurrencyTraders;

public sealed class UpdateCurrencyTraderMessage {
    public UpdateCurrencyTraderMessage(CurrencyTrader currencyTrader) {
        CurrencyTrader = currencyTrader;
    }

    public CurrencyTrader CurrencyTrader { get; set; }
}