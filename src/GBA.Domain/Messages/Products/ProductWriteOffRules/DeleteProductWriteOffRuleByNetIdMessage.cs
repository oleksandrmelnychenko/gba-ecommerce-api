using System;

namespace GBA.Domain.Messages.Products.ProductWriteOffRules;

public sealed class DeleteProductWriteOffRuleByNetIdMessage {
    public DeleteProductWriteOffRuleByNetIdMessage(Guid ruleNetId, Guid userNetId) {
        RuleNetId = ruleNetId;

        UserNetId = userNetId;
    }

    public Guid RuleNetId { get; }

    public Guid UserNetId { get; }
}