namespace GBA.Domain.Entities.ExchangeRates;

public sealed class ExchangeRateHistory : EntityBase {
    public decimal Amount { get; set; }

    public long ExchangeRateId { get; set; }

    public long UpdatedById { get; set; }

    public ExchangeRate ExchangeRate { get; set; }

    public User UpdatedBy { get; set; }
}