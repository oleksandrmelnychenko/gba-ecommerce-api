using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.ClientShoppingCartModels;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ShoppingCartStockRepository : IShoppingCartStockRepository {
    public const int LockTimeoutMilliseconds = 15000;

    internal const string LockCartScopeSql = """
        DECLARE @Result int;
        EXEC @Result = sys.sp_getapplock
            @Resource = @Resource,
            @LockMode = N'Exclusive',
            @LockOwner = N'Transaction',
            @LockTimeout = @Timeout;
        SELECT @Result;
        """;

    internal const string LockCartSql = """
        SELECT [ID]
        FROM [dbo].[ClientShoppingCart] WITH (UPDLOCK, ROWLOCK)
        WHERE [ID] = @Id
          AND [Deleted] = 0
          AND [IsOffer] = 0;
        """;

    internal const string LockProductStockSql = """
        SELECT [ID], [Amount]
        FROM [dbo].[ProductAvailability] WITH (UPDLOCK, ROWLOCK, FORCESEEK)
        WHERE [ProductID] = @ProductId
          AND [Deleted] = 0;
        """;

    internal const string CartItemsSql = """
        SELECT [OrderItem].*, [Product].[ID], [Product].[NetUID], [Product].[VendorCode]
        FROM [dbo].[OrderItem]
        INNER JOIN [dbo].[Product] ON [Product].[ID] = [OrderItem].[ProductID]
        WHERE [OrderItem].[ClientShoppingCartID] = @Id
          AND [OrderItem].[OrderID] IS NULL
          AND [OrderItem].[Deleted] = 0
        ORDER BY [OrderItem].[ID];
        """;

    internal const string ReserveSql = """
        UPDATE [dbo].[ProductAvailability]
        SET [Amount] = [Amount] - @Qty,
            [Updated] = GETUTCDATE()
        WHERE [ID] = @ProductAvailabilityId
          AND [Deleted] = 0
          AND [Amount] >= @Qty;

        IF @@ROWCOUNT = 1
        BEGIN
            UPDATE [dbo].[ProductReservation]
            SET [Qty] = [Qty] + @Qty,
                [Updated] = GETUTCDATE()
            WHERE [ID] = (
                SELECT TOP (1) [ID]
                FROM [dbo].[ProductReservation]
                WHERE [OrderItemID] = @OrderItemId
                  AND [ProductAvailabilityID] = @ProductAvailabilityId
                  AND [ConsignmentItemID] IS NULL
                  AND [IsReSaleReservation] = 0
                  AND [Deleted] = 0
                ORDER BY [ID]);

            IF @@ROWCOUNT = 0
                INSERT INTO [dbo].[ProductReservation]
                    ([OrderItemID], [ProductAvailabilityID], [Qty], [IsReSaleReservation], [Updated])
                VALUES
                    (@OrderItemId, @ProductAvailabilityId, @Qty, 0, GETUTCDATE());

            SELECT CAST(1 AS bit);
        END
        ELSE
            SELECT CAST(0 AS bit);
        """;

    internal const string ReservationsSql = """
        SELECT [ID] AS [Id], [ProductAvailabilityID] AS [ProductAvailabilityId], [Qty]
        FROM [dbo].[ProductReservation]
        WHERE [OrderItemID] = @OrderItemId
          AND [Deleted] = 0
        ORDER BY [ID] DESC;
        """;

    internal const string ReleaseSql = """
        UPDATE [dbo].[ProductReservation]
        SET [Qty] = CASE WHEN [Qty] > @Qty THEN [Qty] - @Qty ELSE [Qty] END,
            [Deleted] = CASE WHEN [Qty] > @Qty THEN 0 ELSE 1 END,
            [Updated] = GETUTCDATE()
        WHERE [ID] = @ReservationId
          AND [ProductAvailabilityID] = @ProductAvailabilityId
          AND [Deleted] = 0
          AND [Qty] >= @Qty;

        IF @@ROWCOUNT = 1
        BEGIN
            UPDATE [dbo].[ProductAvailability]
            SET [Amount] = [Amount] + @Qty,
                [Updated] = GETUTCDATE()
            WHERE [ID] = @ProductAvailabilityId;

            SELECT CAST(1 AS bit);
        END
        ELSE
            SELECT CAST(0 AS bit);
        """;

    internal const string ReleaseAllSql = """
        DECLARE @Released TABLE ([ProductAvailabilityID] bigint NOT NULL, [Qty] float NOT NULL);

        UPDATE [dbo].[ProductReservation]
        SET [Deleted] = 1,
            [Updated] = GETUTCDATE()
        OUTPUT deleted.[ProductAvailabilityID], deleted.[Qty] INTO @Released
        WHERE [OrderItemID] = @OrderItemId
          AND [Deleted] = 0;

        UPDATE [Availability]
        SET [Availability].[Amount] = [Availability].[Amount] + [Released].[Qty],
            [Availability].[Updated] = GETUTCDATE()
        FROM [dbo].[ProductAvailability] AS [Availability]
        INNER JOIN (
            SELECT [ProductAvailabilityID], SUM([Qty]) AS [Qty]
            FROM @Released
            GROUP BY [ProductAvailabilityID]
        ) AS [Released] ON [Released].[ProductAvailabilityID] = [Availability].[ID];

        SELECT COUNT(*) FROM @Released;
        """;

    internal const string IncreaseItemQtySql = """
        UPDATE [dbo].[OrderItem]
        SET [Qty] = [Qty] + @Delta,
            [Updated] = GETUTCDATE()
        WHERE [ID] = @Id
          AND [ClientShoppingCartID] = @ClientShoppingCartId
          AND [OrderID] IS NULL
          AND [Deleted] = 0
          AND [Qty] = @ObservedQty;
        """;

    internal const string SetItemQtySql = """
        UPDATE [dbo].[OrderItem]
        SET [Qty] = @Qty,
            [Updated] = GETUTCDATE()
        WHERE [ID] = @Id
          AND [ClientShoppingCartID] = @ClientShoppingCartId
          AND [OrderID] IS NULL
          AND [Deleted] = 0
          AND [Qty] = @ObservedQty;
        """;

    internal const string ExtendCartSql = """
        UPDATE [Cart]
        SET [Cart].[ValidUntil] = DATEADD(day, [CartClient].[ClearCartAfterDays], @Today),
            [Cart].[Updated] = GETUTCDATE()
        FROM [dbo].[ClientShoppingCart] AS [Cart]
        INNER JOIN [dbo].[ClientAgreement] AS [CartAgreement] ON [CartAgreement].[ID] = [Cart].[ClientAgreementID]
        INNER JOIN [dbo].[Client] AS [CartClient] ON [CartClient].[ID] = [CartAgreement].[ClientID]
        WHERE [Cart].[ID] = @Id
          AND [Cart].[Deleted] = 0
          AND [Cart].[IsOffer] = 0
          AND [Cart].[ValidUntil] < DATEADD(day, [CartClient].[ClearCartAfterDays], @Today);
        """;

    internal const string ExpiredCartPredicateSql = """
        [Cart].[Deleted] = 0
          AND [Cart].[IsOffer] = 0
          AND [CartClient].[ClearCartAfterDays] > 0
          AND [Cart].[ValidUntil] < @Today
          AND NOT EXISTS (
              SELECT 1
              FROM [dbo].[OrderItem] AS [RecentItem]
              WHERE [RecentItem].[ClientShoppingCartID] = [Cart].[ID]
                AND [RecentItem].[OrderID] IS NULL
                AND [RecentItem].[Deleted] = 0
                AND [RecentItem].[Updated] >= DATEADD(day, -[CartClient].[ClearCartAfterDays], @UtcNow))
        """;

    internal const string ExpiredCartsSql = """
        SELECT [Cart].[ID]
        FROM [dbo].[ClientShoppingCart] AS [Cart]
        INNER JOIN [dbo].[ClientAgreement] AS [CartAgreement] ON [CartAgreement].[ID] = [Cart].[ClientAgreementID]
        INNER JOIN [dbo].[Client] AS [CartClient] ON [CartClient].[ID] = [CartAgreement].[ClientID]
        WHERE
        """ + " " + ExpiredCartPredicateSql + """

        ORDER BY [Cart].[ID];
        """;

    internal const string IsExpiredSql = """
        SELECT COUNT(*)
        FROM [dbo].[ClientShoppingCart] AS [Cart]
        INNER JOIN [dbo].[ClientAgreement] AS [CartAgreement] ON [CartAgreement].[ID] = [Cart].[ClientAgreementID]
        INNER JOIN [dbo].[Client] AS [CartClient] ON [CartClient].[ID] = [CartAgreement].[ClientID]
        WHERE [Cart].[ID] = @Id
          AND
        """ + " " + ExpiredCartPredicateSql + ";";

    internal const string RemoveIfEmptySql = """
        UPDATE [dbo].[ClientShoppingCart]
        SET [Deleted] = 1,
            [Updated] = GETUTCDATE()
        WHERE [ID] = @Id
          AND [Deleted] = 0
          AND [IsOffer] = 0
          AND NOT EXISTS (
              SELECT 1
              FROM [dbo].[OrderItem]
              WHERE [OrderItem].[ClientShoppingCartID] = @Id
                AND [OrderItem].[OrderID] IS NULL
                AND [OrderItem].[Deleted] = 0);
        """;

    private readonly IDbConnection _connection;

    public ShoppingCartStockRepository(IDbConnection connection) {
        _connection = connection;
    }

    public static string CartScopeResource(long clientAgreementId, bool isVatCart, long? workplaceId) {
        return string.Join(
            ':',
            "gba-ecommerce-cart",
            clientAgreementId.ToString(CultureInfo.InvariantCulture),
            isVatCart ? "vat" : "no-vat",
            workplaceId?.ToString(CultureInfo.InvariantCulture) ?? "client");
    }

    public bool TryLockCartScope(long clientAgreementId, bool isVatCart, long? workplaceId) {
        return _connection.ExecuteScalar<int>(
            LockCartScopeSql,
            new {
                Resource = CartScopeResource(clientAgreementId, isVatCart, workplaceId),
                Timeout = LockTimeoutMilliseconds
            }) >= 0;
    }

    public bool TryLockCart(long clientShoppingCartId) {
        return _connection.QuerySingleOrDefault<long?>(LockCartSql, new { Id = clientShoppingCartId }) == clientShoppingCartId;
    }

    public void LockProductStock(IEnumerable<long> productIds) {
        foreach (long productId in productIds.Where(id => id > 0).Distinct().OrderBy(id => id))
            _connection.Query(LockProductStockSql, new { ProductId = productId }).AsList();
    }

    public List<OrderItem> GetCartItems(long clientShoppingCartId) {
        return _connection.Query<OrderItem, Product, OrderItem>(
            CartItemsSql,
            (item, product) => {
                item.Product = product;
                return item;
            },
            new { Id = clientShoppingCartId },
            splitOn: "ID").ToList();
    }

    public bool Reserve(long orderItemId, long productAvailabilityId, double qty) {
        return _connection.ExecuteScalar<bool>(
            ReserveSql,
            new { OrderItemId = orderItemId, ProductAvailabilityId = productAvailabilityId, Qty = qty });
    }

    public List<ShoppingCartReservation> GetReservations(long orderItemId) {
        return _connection.Query<ShoppingCartReservation>(ReservationsSql, new { OrderItemId = orderItemId }).ToList();
    }

    public bool Release(ShoppingCartReservation reservation, double qty) {
        return _connection.ExecuteScalar<bool>(
            ReleaseSql,
            new {
                ReservationId = reservation.Id,
                reservation.ProductAvailabilityId,
                Qty = qty
            });
    }

    public int ReleaseAll(long orderItemId) {
        return _connection.ExecuteScalar<int>(ReleaseAllSql, new { OrderItemId = orderItemId });
    }

    public bool IncreaseItemQty(long orderItemId, long clientShoppingCartId, double observedQty, double delta) {
        return _connection.Execute(
            IncreaseItemQtySql,
            new {
                Id = orderItemId,
                ClientShoppingCartId = clientShoppingCartId,
                ObservedQty = observedQty,
                Delta = delta
            }) == 1;
    }

    public bool SetItemQty(long orderItemId, long clientShoppingCartId, double observedQty, double qty) {
        return _connection.Execute(
            SetItemQtySql,
            new {
                Id = orderItemId,
                ClientShoppingCartId = clientShoppingCartId,
                ObservedQty = observedQty,
                Qty = qty
            }) == 1;
    }

    public void ExtendCart(long clientShoppingCartId, DateTime today) {
        _connection.Execute(ExtendCartSql, new { Id = clientShoppingCartId, Today = today.Date });
    }

    public List<long> GetExpiredCartIds(DateTime today, DateTime utcNow) {
        return _connection.Query<long>(ExpiredCartsSql, new { Today = today.Date, UtcNow = utcNow }).ToList();
    }

    public bool IsExpired(long clientShoppingCartId, DateTime today, DateTime utcNow) {
        return _connection.ExecuteScalar<int>(
            IsExpiredSql,
            new { Id = clientShoppingCartId, Today = today.Date, UtcNow = utcNow }) == 1;
    }

    public bool RemoveIfEmpty(long clientShoppingCartId) {
        return _connection.Execute(RemoveIfEmptySql, new { Id = clientShoppingCartId }) == 1;
    }
}
