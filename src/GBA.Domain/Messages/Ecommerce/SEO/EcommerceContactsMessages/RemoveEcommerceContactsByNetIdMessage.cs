using System;

namespace GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactsMessages;

public sealed class RemoveEcommerceContactsByNetIdMessage {
    public RemoveEcommerceContactsByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}