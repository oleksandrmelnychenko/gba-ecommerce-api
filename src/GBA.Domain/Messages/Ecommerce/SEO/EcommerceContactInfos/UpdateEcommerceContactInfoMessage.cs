using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactInfos;

public sealed class UpdateEcommerceContactInfoMessage {
    public UpdateEcommerceContactInfoMessage(EcommerceContactInfo ecommerceContactInfo) {
        EcommerceContactInfo = ecommerceContactInfo;
    }

    public EcommerceContactInfo EcommerceContactInfo { get; }
}