using System;
using System.Collections.Generic;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Messages.ExchangeRates.GovExchangeRates;

public sealed class UpdateGovExchangeRateMessage {
    public UpdateGovExchangeRateMessage(List<GovExchangeRate> govExchangeRates, Guid userNetId) {
        GovExchangeRates = govExchangeRates;

        UserNetId = userNetId;
    }

    public List<GovExchangeRate> GovExchangeRates { get; set; }

    public Guid UserNetId { get; }
}