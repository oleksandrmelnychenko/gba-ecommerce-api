using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Currencies.CurrencyTraders;

public sealed class AddNewCurrencyTraderMessage {
    public AddNewCurrencyTraderMessage(CurrencyTrader currencyTrader) {
        CurrencyTrader = currencyTrader;
    }

    public CurrencyTrader CurrencyTrader { get; set; }
}