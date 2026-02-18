using System;

namespace GBA.Domain.Messages.Currencies;

public sealed class GetCurrencyByNetIdMessage {
    public GetCurrencyByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}