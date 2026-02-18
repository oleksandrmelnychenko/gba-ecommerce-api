using System;

namespace GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactsMessages;

public sealed class GetEcommerceContactsByNetIdMessage {
    public GetEcommerceContactsByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}