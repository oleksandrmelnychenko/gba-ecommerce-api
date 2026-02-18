namespace GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;

public sealed class CalculateConvertedAmountMessage {
    public CalculateConvertedAmountMessage(decimal amount, decimal exchangeRate, long fromCurrencyId, long toCurrencyId) {
        Amount = amount;

        ExchangeRate = exchangeRate;

        FromCurrencyId = fromCurrencyId;

        ToCurrencyId = toCurrencyId;
    }

    public decimal Amount { get; set; }

    public decimal ExchangeRate { get; set; }

    public long FromCurrencyId { get; set; }

    public long ToCurrencyId { get; set; }
}