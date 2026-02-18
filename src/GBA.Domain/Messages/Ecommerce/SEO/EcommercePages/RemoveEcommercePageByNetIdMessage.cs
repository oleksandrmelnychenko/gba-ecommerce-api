using System;

namespace GBA.Domain.Messages.Ecommerce.SEO.EcommercePages;

public sealed class RemoveEcommercePageByNetIdMessage {
    public RemoveEcommercePageByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}