using System;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products.ProductWriteOffRules;

public sealed class AddOrUpdateProductWriteOffRuleMessage {
    public AddOrUpdateProductWriteOffRuleMessage(ProductWriteOffRule productWriteOffRule, Guid userNetId) {
        ProductWriteOffRule = productWriteOffRule;

        UserNetId = userNetId;
    }

    public ProductWriteOffRule ProductWriteOffRule { get; }

    public Guid UserNetId { get; }
}