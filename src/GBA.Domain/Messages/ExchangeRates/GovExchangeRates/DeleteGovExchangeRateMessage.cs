using System;

namespace GBA.Domain.Messages.ExchangeRates.GovExchangeRates;

public sealed class DeleteGovExchangeRateMessage {
    public DeleteGovExchangeRateMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}