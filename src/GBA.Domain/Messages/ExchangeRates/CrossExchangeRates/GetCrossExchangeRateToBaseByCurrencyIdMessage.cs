namespace GBA.Domain.Messages.ExchangeRates;

public sealed class GetCrossExchangeRateToBaseByCurrencyIdMessage {
    public GetCrossExchangeRateToBaseByCurrencyIdMessage(long currencyId) {
        CurrencyId = currencyId;
    }

    public long CurrencyId { get; set; }
}