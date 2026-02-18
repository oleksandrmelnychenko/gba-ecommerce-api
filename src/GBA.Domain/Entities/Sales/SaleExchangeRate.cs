using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Entities.Sales;

public sealed class SaleExchangeRate : EntityBase {
    public long SaleId { get; set; }

    public long ExchangeRateId { get; set; }

    public decimal Value { get; set; }

    public Sale Sale { get; set; }

    public ExchangeRate ExchangeRate { get; set; }
}