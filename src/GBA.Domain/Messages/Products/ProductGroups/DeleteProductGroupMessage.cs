using System;

namespace GBA.Domain.Messages.Products;

public sealed class DeleteProductGroupMessage {
    public DeleteProductGroupMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}