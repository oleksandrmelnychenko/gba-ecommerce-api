using System.Collections.Generic;

namespace GBA.Domain.Entities.ExchangeRates;

public sealed class CrossExchangeRate : EntityBase {
    public CrossExchangeRate() {
        CrossExchangeRateHistories = new HashSet<CrossExchangeRateHistory>();
    }

    public long CurrencyFromId { get; set; }

    public long CurrencyToId { get; set; }

    public decimal Amount { get; set; }

    public string Code { get; set; }

    public string Culture { get; set; }

    public Currency CurrencyFrom { get; set; }

    public Currency CurrencyTo { get; set; }

    public ICollection<CrossExchangeRateHistory> CrossExchangeRateHistories { get; set; }
}