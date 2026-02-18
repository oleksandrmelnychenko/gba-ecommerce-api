using System;

namespace GBA.Domain.Messages.Currencies;

public sealed class DeleteCurrencyMessage {
    public DeleteCurrencyMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}