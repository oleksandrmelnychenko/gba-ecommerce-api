using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactsMessages;

public sealed class AddEcommerceContactsMessage {
    public AddEcommerceContactsMessage(EcommerceContacts ecommerceContacts) {
        EcommerceContacts = ecommerceContacts;
    }

    public EcommerceContacts EcommerceContacts { get; }
}