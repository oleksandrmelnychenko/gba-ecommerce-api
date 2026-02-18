using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;

namespace GBA.Services.Services.Offers.Contracts;

public interface IOfferService {
    Task<ClientShoppingCart> GetOfferByNetId(Guid netId);

    Task<List<ClientShoppingCart>> GetAllAvailableOffersByClientNetId(Guid netId);

    Task<Sale> GenerateNewOrderAndSaleFromOffer(ClientShoppingCart clientShoppingCart, Guid clientNetId, bool addCurrentCartItems);

    Task<Order> DynamicallyCalculateTotalPrices(Order order);
}