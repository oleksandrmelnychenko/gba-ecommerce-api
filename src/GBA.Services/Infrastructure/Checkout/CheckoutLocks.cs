using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;

namespace GBA.Services.Infrastructure.Checkout;

public static class CheckoutLocks {
    internal const string LockShoppingCartSql = """
        SELECT [ID]
        FROM [dbo].[ClientShoppingCart] WITH (UPDLOCK, ROWLOCK)
        WHERE [ID] = @Id
          AND [Deleted] = 0;
        """;

    internal const string LockProductStockSql = """
        SELECT [ID], [Amount]
        FROM [dbo].[ProductAvailability] WITH (UPDLOCK, ROWLOCK, FORCESEEK)
        WHERE [ProductID] = @ProductId
          AND [Deleted] = 0;
        """;

    internal const string ClaimCartItemSql = """
        UPDATE [dbo].[OrderItem]
        SET [ClientShoppingCartID] = NULL,
            [OrderID] = @OrderId,
            [Updated] = GETUTCDATE()
        WHERE [NetUID] = @NetUid
          AND [ClientShoppingCartID] = @ClientShoppingCartId
          AND [OrderID] IS NULL
          AND [Deleted] = 0
          AND [Qty] = @Qty;
        """;

    public static ClientShoppingCart LockShoppingCart(
        IDbConnection connection,
        ClientShoppingCart observedCart,
        Func<ClientShoppingCart> reload) {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(reload);

        if (observedCart == null) return null;

        CheckoutTransaction.RequireAmbient();

        long? lockedCartId = connection.QuerySingleOrDefault<long?>(
            LockShoppingCartSql,
            new { observedCart.Id });

        if (lockedCartId != observedCart.Id)
            throw new CheckoutConflictException(CheckoutConflictException.CartChangedMessage);

        ClientShoppingCart currentCart = reload();

        if (currentCart == null || currentCart.Id != observedCart.Id)
            throw new CheckoutConflictException(CheckoutConflictException.CartChangedMessage);

        return currentCart;
    }

    public static void LockProductStock(IDbConnection connection, IEnumerable<long> productIds) {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(productIds);

        CheckoutTransaction.RequireAmbient();

        foreach (long productId in productIds.Where(id => id > 0).Distinct().OrderBy(id => id))
            connection.Query(LockProductStockSql, new { ProductId = productId }).AsList();
    }

    public static void ClaimCartItem(
        IDbConnection connection,
        long clientShoppingCartId,
        OrderItem cartItem,
        double observedQty,
        long orderId) {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(cartItem);

        CheckoutTransaction.RequireAmbient();

        int claimed = connection.Execute(
            ClaimCartItemSql,
            new {
                cartItem.NetUid,
                ClientShoppingCartId = clientShoppingCartId,
                OrderId = orderId,
                Qty = observedQty
            });

        if (claimed != 1)
            throw new CheckoutConflictException(CheckoutConflictException.CartChangedMessage);

        cartItem.ClientShoppingCartId = null;
        cartItem.OrderId = orderId;
    }

    public static void ClaimCartItems(IDbConnection connection, ClientShoppingCart cart, long orderId) {
        ArgumentNullException.ThrowIfNull(cart);

        foreach (OrderItem cartItem in cart.OrderItems.OrderBy(item => item.Id))
            ClaimCartItem(connection, cart.Id, cartItem, cartItem.Qty, orderId);
    }
}
