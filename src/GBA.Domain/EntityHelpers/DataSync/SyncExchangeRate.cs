using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncExchangeRate {
    public DateTime Date { get; set; }

    public string CurrencyCode { get; set; }

    public decimal RateExchange { get; set; }
}