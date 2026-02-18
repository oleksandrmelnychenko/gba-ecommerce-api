using System;

namespace GBA.Domain.Messages.ExchangeRates;

public sealed class GetExchangeRateBySpecificDateAndTimeMessage {
    public GetExchangeRateBySpecificDateAndTimeMessage(DateTime fromDate, Guid fromCurrencyNetId, Guid toCurrencyNetId) {
        FromDate = fromDate;

        FromCurrencyNetId = fromCurrencyNetId;

        ToCurrencyNetId = toCurrencyNetId;
    }

    public DateTime FromDate { get; set; }

    public Guid FromCurrencyNetId { get; set; }

    public Guid ToCurrencyNetId { get; set; }
}