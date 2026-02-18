using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetProductGroupByNetIdMessage {
    public GetProductGroupByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}