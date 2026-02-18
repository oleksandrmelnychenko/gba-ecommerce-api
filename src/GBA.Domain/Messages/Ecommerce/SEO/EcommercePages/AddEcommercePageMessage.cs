using System;
using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Messages.Ecommerce.SEO.EcommercePages;

public sealed class AddEcommercePageMessage {
    public AddEcommercePageMessage(EcommercePage ecommercePage, Guid userNetId) {
        EcommercePage = ecommercePage;
        UserNetId = userNetId;
    }

    public AddEcommercePageMessage(EcommercePage ecommercePage) : this(ecommercePage, Guid.Empty) { }

    public EcommercePage EcommercePage { get; }

    public Guid UserNetId { get; }
}