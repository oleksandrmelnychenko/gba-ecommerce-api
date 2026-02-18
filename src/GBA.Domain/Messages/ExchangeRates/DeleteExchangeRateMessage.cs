using System;

namespace GBA.Domain.Messages.ExchangeRates;

public sealed class DeleteExchangeRateMessage {
    public DeleteExchangeRateMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}