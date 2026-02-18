namespace GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactInfos;

public sealed class GetEcommerceContactInfoByIdMessage {
    public GetEcommerceContactInfoByIdMessage(long id) {
        Id = id;
    }

    public long Id { get; }
}