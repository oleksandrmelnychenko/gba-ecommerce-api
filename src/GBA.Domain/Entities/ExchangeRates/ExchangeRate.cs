using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.ExchangeRates;

public sealed class ExchangeRate : EntityBase {
    public ExchangeRate() {
        SaleExchangeRates = new HashSet<SaleExchangeRate>();

        ExchangeRateHistories = new HashSet<ExchangeRateHistory>();
    }

    public string Culture { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; }

    public string Code { get; set; }

    public long? CurrencyId { get; set; }

    public Currency AssignedCurrency { get; set; }

    public ICollection<SaleExchangeRate> SaleExchangeRates { get; set; }

    public ICollection<ExchangeRateHistory> ExchangeRateHistories { get; set; }
}