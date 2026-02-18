using System;

namespace GBA.Domain.Messages.Pricings;

public sealed class DeleteProviderPricingMessage {
    public DeleteProviderPricingMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}