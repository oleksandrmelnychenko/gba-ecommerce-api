using System;

namespace GBA.Domain.Messages.Currencies.CurrencyTraders;

public sealed class GetAllCurrencyTraderExchangeRatesByTraderNetIdFilteredMessage {
    public GetAllCurrencyTraderExchangeRatesByTraderNetIdFilteredMessage(Guid netId, DateTime from, DateTime to) {
        NetId = netId;

        From = from;

        To = to;
    }

    public Guid NetId { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }
}