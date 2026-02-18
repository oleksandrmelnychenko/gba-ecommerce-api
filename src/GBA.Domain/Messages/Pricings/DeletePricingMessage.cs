using System;

namespace GBA.Domain.Messages.Pricings;

public sealed class DeletePricingMessage {
    public DeletePricingMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}