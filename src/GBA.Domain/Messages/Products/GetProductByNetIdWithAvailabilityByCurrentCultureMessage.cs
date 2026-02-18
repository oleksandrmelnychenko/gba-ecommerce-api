using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetProductByNetIdWithAvailabilityByCurrentCultureMessage {
    public GetProductByNetIdWithAvailabilityByCurrentCultureMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}