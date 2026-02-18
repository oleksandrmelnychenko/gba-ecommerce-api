using System;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Sales.Offers;

public sealed class ProcessSaleOfferMessage {
    public ProcessSaleOfferMessage(ClientShoppingCart offer, Guid userNetId) {
        Offer = offer;

        UserNetId = userNetId;
    }

    public ClientShoppingCart Offer { get; }

    public Guid UserNetId { get; }
}