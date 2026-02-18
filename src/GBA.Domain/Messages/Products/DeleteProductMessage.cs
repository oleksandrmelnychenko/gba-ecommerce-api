using System;

namespace GBA.Domain.Messages.Products;

public sealed class DeleteProductMessage {
    public DeleteProductMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}