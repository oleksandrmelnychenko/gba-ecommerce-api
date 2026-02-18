using System;

namespace GBA.Domain.Messages.Products.ProductWriteOffRules;

public sealed class GetProductWriteOffRulesByProductGroupNetIdMessage {
    public GetProductWriteOffRulesByProductGroupNetIdMessage(Guid productGroupNetId) {
        ProductGroupNetId = productGroupNetId;
    }

    public Guid ProductGroupNetId { get; }
}