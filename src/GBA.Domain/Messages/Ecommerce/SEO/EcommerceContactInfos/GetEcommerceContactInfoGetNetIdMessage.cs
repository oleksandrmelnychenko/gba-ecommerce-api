using System;

namespace GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactInfos;

public sealed class GetEcommerceContactInfoGetNetIdMessage {
    public GetEcommerceContactInfoGetNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}