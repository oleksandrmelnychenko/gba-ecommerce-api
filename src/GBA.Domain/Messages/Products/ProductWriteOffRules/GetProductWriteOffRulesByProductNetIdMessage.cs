using System;

namespace GBA.Domain.Messages.Products.ProductWriteOffRules;

public sealed class GetProductWriteOffRulesByProductNetIdMessage {
    public GetProductWriteOffRulesByProductNetIdMessage(Guid productNetId) {
        ProductNetId = productNetId;
    }

    public Guid ProductNetId { get; }
}