using System;

namespace GBA.Domain.Entities;

public sealed class CurrencyTraderExchangeRate : EntityBase {
    public string CurrencyName { get; set; }

    public decimal ExchangeRate { get; set; }

    public DateTime FromDate { get; set; }

    public long CurrencyTraderId { get; set; }

    public CurrencyTrader CurrencyTrader { get; set; }
}