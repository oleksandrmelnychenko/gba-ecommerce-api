using System;

namespace GBA.Domain.Messages.Products.ProductCapitalizations;

public sealed class GetProductCapitalizationByNetIdMessage {
    public GetProductCapitalizationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}