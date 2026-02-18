namespace GBA.Domain.Entities.ExchangeRates;

public sealed class GovCrossExchangeRateHistory : EntityBase {
    public decimal Amount { get; set; }

    public long UpdatedById { get; set; }

    public long GovCrossExchangeRateId { get; set; }

    public User UpdatedBy { get; set; }

    public GovCrossExchangeRate CrossExchangeRate { get; set; }
}