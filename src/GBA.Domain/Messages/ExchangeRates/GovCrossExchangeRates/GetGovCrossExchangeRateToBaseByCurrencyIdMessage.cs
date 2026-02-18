namespace GBA.Domain.Messages.ExchangeRates.GovCrossExchangeRates;

public sealed class GetGovCrossExchangeRateToBaseByCurrencyIdMessage {
    public GetGovCrossExchangeRateToBaseByCurrencyIdMessage(long currencyId) {
        CurrencyId = currencyId;
    }

    public long CurrencyId { get; }
}