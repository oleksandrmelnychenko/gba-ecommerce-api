using System;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Messages.ExchangeRates;

public sealed class UpdateCrossExchangeRateMessage {
    public UpdateCrossExchangeRateMessage(CrossExchangeRate crossExchangeRate, Guid userNetId) {
        CrossExchangeRate = crossExchangeRate;

        UserNetId = userNetId;
    }

    public CrossExchangeRate CrossExchangeRate { get; set; }

    public Guid UserNetId { get; set; }
}