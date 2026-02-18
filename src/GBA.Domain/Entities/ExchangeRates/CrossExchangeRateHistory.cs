namespace GBA.Domain.Entities.ExchangeRates;

public sealed class CrossExchangeRateHistory : EntityBase {
    public decimal Amount { get; set; }

    public long UpdatedById { get; set; }

    public long CrossExchangeRateId { get; set; }

    public User UpdatedBy { get; set; }

    public CrossExchangeRate CrossExchangeRate { get; set; }
}