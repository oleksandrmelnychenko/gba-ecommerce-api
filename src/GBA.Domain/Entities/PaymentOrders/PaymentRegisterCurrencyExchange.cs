using System;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.Entities.PaymentOrders;

public sealed class PaymentRegisterCurrencyExchange : EntityBase {
    public string IncomeNumber { get; set; }

    public string Number { get; set; }

    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public decimal Amount { get; set; }

    public decimal ExchangeRate { get; set; }

    public bool IsUpdated { get; set; }

    public bool IsCanceled { get; set; }

    public PaymentRegisterTransferType Type { get; set; }

    public long FromPaymentCurrencyRegisterId { get; set; }

    public long ToPaymentCurrencyRegisterId { get; set; }

    public long UserId { get; set; }

    public long? CurrencyTraderId { get; set; }

    public PaymentCurrencyRegister FromPaymentCurrencyRegister { get; set; }

    public PaymentCurrencyRegister ToPaymentCurrencyRegister { get; set; }

    public User User { get; set; }

    public CurrencyTrader CurrencyTrader { get; set; }

    public PaymentMovementOperation PaymentMovementOperation { get; set; }
}