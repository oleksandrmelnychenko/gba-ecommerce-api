using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Currencies;

public sealed class UpdateCurrencyMessage {
    public UpdateCurrencyMessage(Currency currency) {
        Currency = currency;
    }

    public Currency Currency { get; set; }
}