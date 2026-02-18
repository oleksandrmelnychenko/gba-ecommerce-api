using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetAllProductGroupsByProductNetIdMessage {
    public GetAllProductGroupsByProductNetIdMessage(Guid productNetId) {
        ProductNetId = productNetId;
    }

    public Guid ProductNetId { get; }
}