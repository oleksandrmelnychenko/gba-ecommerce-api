using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetSupplyOrderByNetIdIfExistsMessage {
    public GetSupplyOrderByNetIdIfExistsMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}