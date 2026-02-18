using System;

namespace GBA.Domain.Messages.Pricings;

public sealed class GetProviderPricingByNetIdMessage {
    public GetProviderPricingByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}