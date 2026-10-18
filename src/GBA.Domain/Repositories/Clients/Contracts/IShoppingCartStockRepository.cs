using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.ClientShoppingCartModels;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IShoppingCartStockRepository {
    bool TryLockCartScope(long clientAgreementId, bool isVatCart, long? workplaceId);

    bool TryLockCart(long clientShoppingCartId);

    void LockProductStock(IEnumerable<long> productIds);

    List<OrderItem> GetCartItems(long clientShoppingCartId);

    bool Reserve(long orderItemId, long productAvailabilityId, double qty);

    List<ShoppingCartReservation> GetReservations(long orderItemId);

    bool Release(ShoppingCartReservation reservation, double qty);

    int ReleaseAll(long orderItemId);

    bool IncreaseItemQty(long orderItemId, long clientShoppingCartId, double observedQty, double delta);

    bool SetItemQty(long orderItemId, long clientShoppingCartId, double observedQty, double qty);

    void ExtendCart(long clientShoppingCartId, DateTime today);

    List<long> GetExpiredCartIds(DateTime today, DateTime utcNow);

    bool IsExpired(long clientShoppingCartId, DateTime today, DateTime utcNow);

    bool RemoveIfEmpty(long clientShoppingCartId);
}
