using System;

namespace GBA.Domain.Messages.Supplies;

public class AddAdditionalPaymentsToSupplyOrderMessage {
    public AddAdditionalPaymentsToSupplyOrderMessage(
        Guid supplyOrderNetId,
        Guid currencyNetId,
        decimal additionalAmount,
        double additionalPercent,
        DateTime fromDate,
        Guid userNetId) {
        SupplyOrderNetId = supplyOrderNetId;
        CurrencyNetId = currencyNetId;
        AdditionalAmount = additionalAmount;
        AdditionalPercent = additionalPercent;
        UserNetId = userNetId;
        FromDate = fromDate.Year.Equals(1) ? DateTime.UtcNow : TimeZoneInfo.ConvertTimeToUtc(fromDate);
    }

    public Guid SupplyOrderNetId { get; }
    public Guid CurrencyNetId { get; }
    public decimal AdditionalAmount { get; }
    public double AdditionalPercent { get; }
    public Guid UserNetId { get; }
    public DateTime FromDate { get; }
}