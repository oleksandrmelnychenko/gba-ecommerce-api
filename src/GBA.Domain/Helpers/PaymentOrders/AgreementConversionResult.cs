namespace GBA.Domain.Helpers.PaymentOrders;

public sealed class AgreementConversionResult {
    public AgreementConversionResult(decimal inAgreementCurrencyAmount, decimal exchangeRate) {
        InAgreementCurrencyAmount = inAgreementCurrencyAmount;
        ExchangeRate = exchangeRate;
    }

    public decimal InAgreementCurrencyAmount { get; }
    public decimal ExchangeRate { get; }
}