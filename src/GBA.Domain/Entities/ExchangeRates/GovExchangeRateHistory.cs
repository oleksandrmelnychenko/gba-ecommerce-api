namespace GBA.Domain.Entities.ExchangeRates;

public class GovExchangeRateHistory : EntityBase {
    public decimal Amount { get; set; }

    public long GovExchangeRateId { get; set; }

    public long UpdatedById { get; set; }

    public GovExchangeRate GovExchangeRate { get; set; }

    public User UpdatedBy { get; set; }
}