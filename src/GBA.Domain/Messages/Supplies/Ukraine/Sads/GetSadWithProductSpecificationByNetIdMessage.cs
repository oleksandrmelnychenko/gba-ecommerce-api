using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.Sads;

public sealed class GetSadWithProductSpecificationByNetIdMessage {
    public GetSadWithProductSpecificationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}