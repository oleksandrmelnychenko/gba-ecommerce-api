using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactInfos;

public sealed class AddEcommerceContactInfoMessage {
    public AddEcommerceContactInfoMessage(EcommerceContactInfo ecommerceContactInfo) {
        EcommerceContactInfo = ecommerceContactInfo;
    }

    public EcommerceContactInfo EcommerceContactInfo { get; }
}