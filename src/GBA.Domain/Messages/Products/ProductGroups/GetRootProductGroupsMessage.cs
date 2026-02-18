using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetRootProductGroupsMessage {
    public GetRootProductGroupsMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}