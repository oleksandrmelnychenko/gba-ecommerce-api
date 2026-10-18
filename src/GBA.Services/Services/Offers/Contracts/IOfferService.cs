using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Services.Infrastructure.Checkout;

namespace GBA.Services.Services.Offers.Contracts;

public interface IOfferService {
    Task<ClientShoppingCart> GetOfferByNetId(Guid netId, Guid actorNetId);

    Task<ClientShoppingCart> GetOfferForPublicLink(Guid netId);

    Task<List<ClientShoppingCart>> GetAllAvailableOffersByClientNetId(Guid netId);

    Task<CheckoutResult> GenerateNewOrderAndSaleFromOffer(
        ClientShoppingCart clientShoppingCart,
        Guid clientNetId,
        bool addCurrentCartItems,
        CheckoutAttempt attempt);

    Task<Order> DynamicallyCalculateTotalPrices(Order order);
}
