using System;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Sales.Offers;

public sealed class AddNewSaleOfferMessage {
    public AddNewSaleOfferMessage(ClientShoppingCart clientShoppingCart, Guid userNetId, int validDays) {
        ClientShoppingCart = clientShoppingCart;

        UserNetId = userNetId;

        ValidDays = validDays;
    }

    public ClientShoppingCart ClientShoppingCart { get; }

    public Guid UserNetId { get; }

    public int ValidDays { get; }
}