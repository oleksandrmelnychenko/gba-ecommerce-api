using System;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Messages.ExchangeRates.GovCrossExchangeRates;

public class UpdateGovCrossExchangeRateMessage {
    public UpdateGovCrossExchangeRateMessage(GovCrossExchangeRate govCrossExchangeRate, Guid userNetId) {
        GovCrossExchangeRate = govCrossExchangeRate;

        UserNetId = userNetId;
    }

    public GovCrossExchangeRate GovCrossExchangeRate { get; }

    public Guid UserNetId { get; }
}