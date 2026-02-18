namespace GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactsMessages;

public sealed class GetEcommerceContactsByIdMessage {
    public GetEcommerceContactsByIdMessage(long id) {
        Id = id;
    }

    public long Id { get; }
}