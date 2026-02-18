using System;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Messages.ExchangeRates;

public sealed class UpdateExchangeRateMessage {
    public UpdateExchangeRateMessage(ExchangeRate exchangeRate, Guid userNetId) {
        ExchangeRate = exchangeRate;

        UserNetId = userNetId;
    }

    public ExchangeRate ExchangeRate { get; set; }

    public Guid UserNetId { get; set; }
}