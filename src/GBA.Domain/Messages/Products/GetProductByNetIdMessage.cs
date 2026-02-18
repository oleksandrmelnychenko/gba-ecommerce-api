using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetProductByNetIdMessage {
    public GetProductByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}