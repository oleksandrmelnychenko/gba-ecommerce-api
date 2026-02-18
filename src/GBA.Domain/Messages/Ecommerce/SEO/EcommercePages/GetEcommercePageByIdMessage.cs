namespace GBA.Domain.Messages.Ecommerce.SEO.EcommercePages;

public sealed class GetEcommercePageByIdMessage {
    public GetEcommercePageByIdMessage(long id) {
        Id = id;
    }

    public long Id { get; }
}