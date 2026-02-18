using System;

namespace GBA.Domain.Messages.Supplies.Returns;

public sealed class GetSupplyReturnByNetIdMessage {
    public GetSupplyReturnByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}