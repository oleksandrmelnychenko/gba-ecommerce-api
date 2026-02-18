namespace GBA.Domain.Messages.Currencies.CurrencyTraders;

public sealed class GetAllCurrencyTradersFromSearchMessage {
    public GetAllCurrencyTradersFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; set; }
}