using System;

namespace GBA.Domain.Messages.ExchangeRates.GovExchangeRates;

public sealed class GetGovExchangeRateBySpecificDateAndTimeMessage {
    public GetGovExchangeRateBySpecificDateAndTimeMessage(DateTime fromDate, Guid fromCurrencyNetId, Guid toCurrencyNetId) {
        FromDate = fromDate;

        FromCurrencyNetId = fromCurrencyNetId;

        ToCurrencyNetId = toCurrencyNetId;
    }

    public DateTime FromDate { get; set; }

    public Guid FromCurrencyNetId { get; set; }

    public Guid ToCurrencyNetId { get; set; }
}