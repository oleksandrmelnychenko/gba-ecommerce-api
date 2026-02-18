using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Messages.Ecommerce.SEO.EcommercePages;

public sealed class UpdateEcommercePageMessage {
    public UpdateEcommercePageMessage(EcommercePage ecommercePage) {
        EcommercePage = ecommercePage;
    }

    public EcommercePage EcommercePage { get; }
}