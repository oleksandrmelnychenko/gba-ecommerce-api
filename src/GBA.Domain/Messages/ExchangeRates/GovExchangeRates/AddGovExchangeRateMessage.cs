using System;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Messages.ExchangeRates.GovExchangeRates;

public sealed class AddGovExchangeRateMessage {
    public AddGovExchangeRateMessage(GovExchangeRate govExchangeRate, Guid userNetId) {
        GovExchangeRate = govExchangeRate;

        UserNetId = userNetId;
    }

    public GovExchangeRate GovExchangeRate { get; set; }

    public Guid UserNetId { get; }
}