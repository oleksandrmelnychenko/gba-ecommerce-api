using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncCrossExchangeRate {
    public DateTime Date { get; set; }

    public string FromCurrencyCode { get; set; }

    public string ToCurrencyCode { get; set; }

    public decimal RateExchange { get; set; }
}