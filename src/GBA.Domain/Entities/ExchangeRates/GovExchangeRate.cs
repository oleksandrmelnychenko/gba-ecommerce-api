using System.Collections.Generic;

namespace GBA.Domain.Entities.ExchangeRates;

public sealed class GovExchangeRate : EntityBase {
    public GovExchangeRate() {
        GovExchangeRateHistories = new HashSet<GovExchangeRateHistory>();
    }

    public string Culture { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; }

    public string Code { get; set; }

    public long? CurrencyId { get; set; }

    public Currency AssignedCurrency { get; set; }

    public ICollection<GovExchangeRateHistory> GovExchangeRateHistories { get; set; }
}