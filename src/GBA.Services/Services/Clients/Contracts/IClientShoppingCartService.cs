using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Sales;

namespace GBA.Services.Services.Clients.Contracts;

public interface IClientShoppingCartService {
    Task<OrderItem> Add(OrderItem orderItem, Guid clientNetId, bool withVat);

    Task<List<OrderItem>> Add(List<OrderItem> orderItems, Guid clientNetId, bool withVat);

    Task<OrderItem> Update(OrderItem orderItem, Guid clientNetId, bool withVat);

    Task<List<OrderItem>> Update(List<OrderItem> orderItems, Guid clientNetId, bool withVat);

    Task<IEnumerable<OrderItem>> GetAllItemsFromCurrentShoppingCartByClientNetId(Guid netId, bool withVat);

    Task DeleteItemFromShoppingCartByNetId(Guid itemNetId, Guid clientNetId, bool withVat);

    Task DeleteAllItemsFromShoppingCartByClientNetId(Guid clientNetId, bool withVat);

    Task<Tuple<bool, string>> VerifyProductAvailability(OrderItem orderItem);

    /// <summary>Releases reservations of expired carts back into ProductAvailability.Amount and removes
    /// those carts. Returns the number of reservations released.</summary>
    Task<int> ReleaseExpiredCartsAsync();
}