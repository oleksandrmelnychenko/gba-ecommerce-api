using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetWithRootGroupsProductGroupMessage {
    public GetWithRootGroupsProductGroupMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}