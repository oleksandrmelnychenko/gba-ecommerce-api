using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Currencies;

public sealed class AddCurrencyMessage {
    public AddCurrencyMessage(Currency currency) {
        Currency = currency;
    }

    public Currency Currency { get; set; }
}