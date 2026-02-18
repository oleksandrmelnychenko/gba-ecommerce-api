using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactsMessages;

public sealed class UpdateEcommerceContactsMessage {
    public UpdateEcommerceContactsMessage(EcommerceContacts ecommerceContacts) {
        EcommerceContacts = ecommerceContacts;
    }

    public EcommerceContacts EcommerceContacts { get; }
}