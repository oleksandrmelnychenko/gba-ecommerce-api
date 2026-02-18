using System;

namespace GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactInfos;

public sealed class RemoveEcommerceContactInfoByNetIdMessage {
    public RemoveEcommerceContactInfoByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}