namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class CalculateCurrencyExchangeResultAmountMessage {
    public CalculateCurrencyExchangeResultAmountMessage(decimal amount, decimal exchangeRate, string currencyCode) {
        Amount = amount;

        ExchangeRate = exchangeRate;

        CurrencyCode = currencyCode;
    }

    public decimal Amount { get; set; }

    public decimal ExchangeRate { get; set; }

    public string CurrencyCode { get; set; }
}