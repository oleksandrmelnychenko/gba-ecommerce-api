using System;

namespace GBA.Domain.Messages.Pricings;

public sealed class GetPricingByNetIdMessage {
    public GetPricingByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}