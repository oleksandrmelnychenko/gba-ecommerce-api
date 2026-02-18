using System.Collections.Generic;

namespace GBA.Domain.Entities.ExchangeRates;

public sealed class GovCrossExchangeRate : EntityBase {
    public GovCrossExchangeRate() {
        GovCrossExchangeRateHistories = new HashSet<GovCrossExchangeRateHistory>();
    }

    public long CurrencyFromId { get; set; }

    public long CurrencyToId { get; set; }

    public decimal Amount { get; set; }

    public string Code { get; set; }

    public string Culture { get; set; }

    public Currency CurrencyFrom { get; set; }

    public Currency CurrencyTo { get; set; }

    public ICollection<GovCrossExchangeRateHistory> GovCrossExchangeRateHistories { get; set; }

    public GovExchangeRate GovExchangeRate { get; set; } // as data transfer
}