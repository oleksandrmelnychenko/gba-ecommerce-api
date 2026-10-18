using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Search;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Agreements;
using GBA.Domain.Repositories.Clients;
using GBA.Domain.Repositories.Currencies;
using GBA.Domain.Repositories.ExchangeRates;
using GBA.Domain.Repositories.Organizations;
using GBA.Domain.Repositories.Pricings;
using GBA.Domain.Repositories.Products;
using GBA.Domain.Repositories.Sales;
using GBA.Domain.Repositories.Storages;
using GBA.Services.Infrastructure.Checkout;
using GBA.Services.Services.Clients;
using GBA.Services.Services.Products;
using GBA.Domain.Repositories.Delivery;
using GBA.Domain.Repositories.Clients.RetailClients;
using GBA.Domain.Repositories.Users;
using GBA.Services.Services.Messengers;
using GBA.Services.Services.Orders;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Api.Tests;

[Collection(CheckoutSqlCollection.Name)]
public sealed class CartStockSqlTests(CheckoutSqlFixture fixture) {
    private static readonly object ScenarioGate = new();
    private static CartScenario? _scenario;

    [Fact]
    public async Task Concurrent_adds_from_different_clients_never_reserve_more_than_the_free_stock() {
        if (!Ready(out CartScenario scenario)) return;

        List<Shopper> shoppers = scenario.Shoppers;
        foreach (Shopper shopper in shoppers) await ClearCart(shopper);
        SetFreeStock(scenario, 3);

        using Barrier start = new(shoppers.Count);
        Task<OrderItem>[] adds = shoppers.Select(shopper => Task.Run(() => {
            start.SignalAndWait();
            UseUkrainianCulture();
            return NewCartService().Add(NewLine(scenario.ProductNetUid, 1), shopper.ClientNetUid, false);
        })).ToArray();
        await Task.WhenAll(adds.Select(task => task.ContinueWith(_ => { })));

        Assert.Equal(3, adds.Count(task => task.IsCompletedSuccessfully));
        Assert.All(
            adds.Where(task => task.IsFaulted),
            task => Assert.True(
                task.Exception!.InnerException is LocalizedException or CheckoutConflictException,
                task.Exception.ToString()));
        Assert.Equal(0d, FreeStock(scenario));
        Assert.Equal(3d, ReservedInCarts(scenario, shoppers));
        Assert.Equal(3d, QuantityInCarts(scenario, shoppers));

        foreach (Shopper shopper in shoppers) await ClearCart(shopper);
        Assert.Equal(3d, FreeStock(scenario));
        Assert.Equal(0d, ReservedInCarts(scenario, shoppers));
    }

    [Fact]
    public async Task Racing_adds_and_quantity_changes_on_one_cart_keep_the_line_and_the_stock_in_step() {
        if (!Ready(out CartScenario scenario)) return;

        Shopper shopper = scenario.Shoppers[0];
        await ClearCart(shopper);
        SetFreeStock(scenario, 40);
        OrderItem line = await NewCartService().Add(NewLine(scenario.ProductNetUid, 2), shopper.ClientNetUid, false);

        using Barrier start = new(12);
        Task[] operations = Enumerable.Range(0, 12).Select(index => Task.Run(async () => {
            start.SignalAndWait();
            UseUkrainianCulture();
            ClientShoppingCartService service = NewCartService();
            if (index % 3 == 0)
                await service.Add(NewLine(scenario.ProductNetUid, 1), shopper.ClientNetUid, false);
            else
                await service.Update(new OrderItem { Id = line.Id, Qty = 1 + index, OverLordQty = 1 + index }, shopper.ClientNetUid, false);
        })).ToArray();
        await Task.WhenAll(operations.Select(task => task.ContinueWith(_ => { })));

        Assert.All(
            operations.Where(task => task.IsFaulted),
            task => Assert.IsType<CheckoutConflictException>(task.Exception!.InnerException));
        double lineQty = QuantityInCarts(scenario, new List<Shopper> { shopper });
        Assert.True(lineQty > 0);
        Assert.Equal(lineQty, ReservedInCarts(scenario, new List<Shopper> { shopper }));
        Assert.Equal(40d - lineQty, FreeStock(scenario));

        await ClearCart(shopper);
        Assert.Equal(40d, FreeStock(scenario));
    }

    [Fact]
    public async Task Adding_many_items_returns_the_lines_and_rolls_back_as_a_whole() {
        if (!Ready(out CartScenario scenario)) return;

        Shopper shopper = scenario.Shoppers[0];
        await ClearCart(shopper);
        SetFreeStock(scenario, 10);

        List<OrderItem> lines = await NewCartService().Add(
            new List<OrderItem> { NewLine(scenario.ProductNetUid, 2), NewLine(scenario.ProductNetUid, 1) },
            shopper.ClientNetUid,
            false);

        Assert.Single(lines);
        Assert.Equal(3d, QuantityInCarts(scenario, new List<Shopper> { shopper }));
        Assert.Equal(3d, ReservedInCarts(scenario, new List<Shopper> { shopper }));
        Assert.Equal(7d, FreeStock(scenario));

        await Assert.ThrowsAsync<ArgumentException>(() => NewCartService().Add(
            new List<OrderItem> { NewLine(scenario.ProductNetUid, 1), NewLine(scenario.HiddenProductNetUid, 1) },
            shopper.ClientNetUid,
            false));

        Assert.Equal(3d, QuantityInCarts(scenario, new List<Shopper> { shopper }));
        Assert.Equal(7d, FreeStock(scenario));

        await ClearCart(shopper);
    }

    [Fact]
    public async Task Using_a_cart_extends_it_and_expiry_releases_only_idle_shop_carts() {
        if (!Ready(out CartScenario scenario)) return;

        Shopper idle = scenario.Shoppers[0];
        Shopper active = scenario.Shoppers[1];
        foreach (Shopper shopper in new[] { idle, active }) await ClearCart(shopper);
        SetFreeStock(scenario, 10);

        await NewCartService().Add(NewLine(scenario.ProductNetUid, 2), idle.ClientNetUid, false);
        await NewCartService().Add(NewLine(scenario.ProductNetUid, 1), active.ClientNetUid, false);
        Assert.Equal(DateTime.Now.Date.AddDays(3), CartValidUntil(idle));

        AgeCart(idle, itemAgeDays: 10);
        AgeCart(active, itemAgeDays: 10);
        await NewCartService().Add(NewLine(scenario.ProductNetUid, 1), active.ClientNetUid, false);
        Assert.Equal(DateTime.Now.Date.AddDays(3), CartValidUntil(active));
        AgeCart(active, itemAgeDays: 0);

        long offerId = CreateExpiredOfferWithReservation(scenario, idle, 1);
        double freeBefore = FreeStock(scenario);

        await NewCartService().ReleaseExpiredCartsAsync();

        Assert.Equal(freeBefore + 2d, FreeStock(scenario));
        Assert.Equal(0d, QuantityInCarts(scenario, new List<Shopper> { idle }));
        Assert.Equal(2d, QuantityInCarts(scenario, new List<Shopper> { active }));
        Assert.Equal(2d, ReservedInCarts(scenario, new List<Shopper> { active }));
        Assert.Null(CartValidUntil(idle));
        Assert.False(CartDeleted(offerId));
        Assert.Equal(1d, ReservedInCart(offerId));

        DeleteOffer(offerId);
        await ClearCart(active);
    }

    [Fact]
    public async Task Signed_in_stock_and_reservations_ignore_the_organizations_other_storages() {
        if (!Ready(out CartScenario scenario)) return;

        Shopper shopper = scenario.Shoppers[0];
        await ClearCart(shopper);
        SetFreeStock(scenario, 2);
        long otherAvailabilityId = PutStockInAnotherOrganizationStorage(scenario, 50);
        try {
            ProductService products = NewProductService();

            Assert.Equal(2d, products.GetSellableQuantities(new List<long> { scenario.ProductId }, shopper.ClientNetUid)[scenario.ProductId]);
            Product detail = await products.GetByNetId(scenario.ProductNetUid, shopper.ClientNetUid, false);
            Assert.Equal(2d, detail.AvailableQtyUk + detail.AvailableQtyUkVAT);

            await Assert.ThrowsAsync<LocalizedException>(() =>
                NewCartService().Add(NewLine(scenario.ProductNetUid, 3), shopper.ClientNetUid, false));
            await NewCartService().Add(NewLine(scenario.ProductNetUid, 2), shopper.ClientNetUid, false);

            OrderItem line = Assert.Single(await NewCartService().GetAllItemsFromCurrentShoppingCartByClientNetId(shopper.ClientNetUid, false));
            Assert.Equal(0d, line.Product.AvailableQtyUk);
            Assert.Equal(50d, AvailabilityAmount(otherAvailabilityId));
            Assert.Equal(0d, AvailabilityAmount(scenario.AvailabilityId));
        } finally {
            await ClearCart(shopper);
            SetAvailabilityAmount(otherAvailabilityId, 0);
        }
    }

    [Fact]
    public async Task A_client_without_a_selected_agreement_checks_out_the_cart_it_sees_without_writing_the_agreement() {
        if (!Ready(out CartScenario scenario)) return;

        Shopper shopper = scenario.Shoppers[2];
        await ClearCart(shopper);
        SetFreeStock(scenario, 5);
        SetSelected(shopper, false);
        try {
            OrderItem item = await NewCartService().Add(NewLine(scenario.ProductNetUid, 1), shopper.ClientNetUid, false);
            Assert.Single(await NewCartService().GetAllItemsFromCurrentShoppingCartByClientNetId(shopper.ClientNetUid, false));
            Assert.False(IsSelected(shopper));

            Sale invoice = new() { Comment = "default agreement test", Order = new Order() };
            invoice.Order.OrderItems.Add(new OrderItem { NetUid = item.NetUid, Qty = 1 });
            CheckoutResult result = await NewOrderService().GenerateNewSaleWithInvoice(
                invoice,
                shopper.ClientNetUid,
                false,
                false,
                new CheckoutAttempt(Guid.NewGuid(), CheckoutFingerprint.Compute(Guid.NewGuid().ToString())));

            Assert.Equal(shopper.ClientAgreementId, SaleClientAgreementId(result.Sale.Id));
            Assert.False(IsSelected(shopper));
        } finally {
            SetSelected(shopper, true);
            await ClearCart(shopper);
        }
    }

    [Fact]
    public void The_default_agreement_prefers_a_priced_then_an_active_agreement_and_skips_the_sync_placeholder() {
        if (!Ready(out _)) return;

        using SqlConnection connection = fixture.Open();
        (long ClientId, Guid ClientNetUid)? client = connection.QueryFirstOrDefault<(long, Guid)?>("""
            SELECT TOP (1) c.ID, c.NetUID
            FROM dbo.Client c
            JOIN dbo.ClientAgreement ca ON ca.ClientID = c.ID AND ca.Deleted = 0
            JOIN dbo.Agreement a ON a.ID = ca.AgreementID AND a.Deleted = 0 AND a.ForReSale = 0
            WHERE c.Deleted = 0
              AND NOT EXISTS (SELECT 1 FROM dbo.ClientSubClient l WHERE l.SubClientID = c.ID AND l.Deleted = 0)
            GROUP BY c.ID, c.NetUID
            HAVING COUNT(*) = 2
            ORDER BY c.ID;
            """);
        if (client == null) return;

        List<AgreementFlags> original = connection.Query<AgreementFlags>("""
            SELECT a.ID AS AgreementId, ca.ID AS ClientAgreementId, a.IsActive, a.IsSelected, a.PricingID AS PricingId, a.IsDefaultForSyncConsignment
            FROM dbo.ClientAgreement ca
            JOIN dbo.Agreement a ON a.ID = ca.AgreementID AND a.Deleted = 0 AND a.ForReSale = 0
            WHERE ca.ClientID = @ClientId AND ca.Deleted = 0
            ORDER BY ca.ID;
            """, new { client.Value.ClientId }).ToList();
        long pricingId = connection.ExecuteScalar<long>("SELECT TOP (1) ID FROM dbo.Pricing WHERE Deleted = 0 ORDER BY ID;");
        AgreementFlags first = original[0];
        AgreementFlags second = original[1];
        ClientAgreementRepository repository = new(connection);

        try {
            SetFlags(connection, first.AgreementId, isActive: true, pricingId: null, placeholder: false);
            SetFlags(connection, second.AgreementId, isActive: false, pricingId: pricingId, placeholder: false);
            Assert.Equal(second.ClientAgreementId, repository.GetSelectedByClientNotSelectedNetId(client.Value.ClientNetUid).Id);

            SetFlags(connection, second.AgreementId, isActive: false, pricingId: null, placeholder: false);
            Assert.Equal(first.ClientAgreementId, repository.GetSelectedByClientNotSelectedNetId(client.Value.ClientNetUid).Id);

            SetFlags(connection, first.AgreementId, isActive: true, pricingId: pricingId, placeholder: true);
            SetFlags(connection, second.AgreementId, isActive: false, pricingId: null, placeholder: false);
            Assert.Equal(second.ClientAgreementId, repository.GetSelectedByClientNotSelectedNetId(client.Value.ClientNetUid).Id);
        } finally {
            foreach (AgreementFlags flags in original)
                connection.Execute("""
                    UPDATE dbo.Agreement
                    SET IsActive = @IsActive, IsSelected = @IsSelected, PricingID = @PricingId, IsDefaultForSyncConsignment = @IsDefaultForSyncConsignment
                    WHERE ID = @AgreementId;
                    """, flags);
        }
    }

    private bool Ready(out CartScenario scenario) {
        scenario = null!;
        if (!fixture.IsConfigured || !fixture.HasConcordSchema) return false;

        UseUkrainianCulture();
        lock (ScenarioGate) {
            _scenario ??= DiscoverScenario();
        }

        scenario = _scenario;
        return true;
    }

    private static void UseUkrainianCulture() {
        CultureInfo culture = new("uk-UA");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private CartScenario DiscoverScenario() {
        using SqlConnection connection = fixture.Open();
        foreach (long organizationId in connection.Query<long>("""
                     SELECT o.ID FROM dbo.Organization o
                     JOIN dbo.Storage s ON s.ID = o.StorageID AND s.Deleted = 0 AND s.ForDefective = 0
                     WHERE o.Deleted = 0
                     ORDER BY o.ID;
                     """).ToList()) {
            List<Shopper> shoppers = connection.Query<Shopper>("""
                SELECT TOP (8) c.ID AS ClientId, c.NetUID AS ClientNetUid, a.ID AS AgreementId, ca.ID AS ClientAgreementId
                FROM dbo.Client c
                JOIN dbo.ClientAgreement ca ON ca.ClientID = c.ID AND ca.Deleted = 0
                JOIN dbo.Agreement a ON a.ID = ca.AgreementID AND a.Deleted = 0 AND a.IsActive = 1
                    AND a.ForReSale = 0 AND a.CurrencyID IS NOT NULL AND a.WithVATAccounting = 0
                    AND a.PricingID IS NOT NULL AND a.OrganizationID = @organizationId
                WHERE c.Deleted = 0
                  AND NOT EXISTS (SELECT 1 FROM dbo.ClientSubClient l WHERE l.SubClientID = c.ID AND l.Deleted = 0)
                  AND NOT EXISTS (SELECT 1 FROM dbo.Workplace w WHERE w.NetUID = c.NetUID)
                  AND (SELECT COUNT(*) FROM dbo.ClientAgreement other
                       JOIN dbo.Agreement otherAgreement ON otherAgreement.ID = other.AgreementID
                       WHERE other.ClientID = c.ID AND other.Deleted = 0 AND otherAgreement.Deleted = 0) = 1
                ORDER BY c.ID;
                """, new { organizationId }).ToList();
            if (shoppers.Count < 8) continue;

            foreach (Shopper shopper in shoppers) SelectAgreement(shopper);

            List<(long Id, Guid NetUid, long AvailabilityId)> candidates = connection.Query<(long, Guid, long)>("""
                SELECT TOP (25) p.ID, p.NetUID, pa.ID
                FROM dbo.Organization o
                JOIN dbo.ProductAvailability pa ON pa.StorageID = o.StorageID AND pa.Deleted = 0 AND pa.Amount >= 20
                JOIN dbo.Product p ON p.ID = pa.ProductID AND p.Deleted = 0 AND p.IsForWeb = 1
                WHERE o.ID = @organizationId
                ORDER BY pa.Amount DESC, p.ID;
                """, new { organizationId }).ToList();

            foreach ((long productId, Guid productNetUid, long availabilityId) in candidates) {
                if (!IsPurchasable(shoppers[0], productNetUid)) continue;

                Guid hidden = connection.ExecuteScalar<Guid>(
                    "SELECT TOP (1) NetUID FROM dbo.Product WHERE Deleted = 0 AND IsForWeb = 0 ORDER BY ID;");
                connection.Execute(
                    "UPDATE dbo.Client SET ClearCartAfterDays = 3 WHERE ID IN @ids;",
                    new { ids = shoppers.Select(shopper => shopper.ClientId) });
                return new CartScenario(organizationId, productId, productNetUid, availabilityId, hidden, shoppers);
            }
        }

        throw new InvalidOperationException("No organization with eight single-agreement clients and a purchasable stocked product was found.");
    }

    private bool IsPurchasable(Shopper shopper, Guid productNetUid) {
        ClientShoppingCartService service = NewCartService();
        try {
            service.DeleteAllItemsFromShoppingCartByClientNetId(shopper.ClientNetUid, false).GetAwaiter().GetResult();
            service.Add(NewLine(productNetUid, 1), shopper.ClientNetUid, false).GetAwaiter().GetResult();
            return true;
        } catch (Exception) {
            return false;
        } finally {
            service.DeleteAllItemsFromShoppingCartByClientNetId(shopper.ClientNetUid, false).GetAwaiter().GetResult();
        }
    }

    private void SelectAgreement(Shopper shopper) {
        using SqlConnection connection = fixture.Open();
        connection.Execute("""
            UPDATE a
            SET a.IsSelected = CASE WHEN a.ID = @AgreementId THEN 1 ELSE 0 END
            FROM dbo.Agreement a
            JOIN dbo.ClientAgreement ca ON ca.AgreementID = a.ID
            WHERE ca.ClientID = @ClientId;
            """, new { shopper.AgreementId, shopper.ClientId });
    }

    private Task ClearCart(Shopper shopper) {
        return NewCartService().DeleteAllItemsFromShoppingCartByClientNetId(shopper.ClientNetUid, false);
    }

    private void SetFreeStock(CartScenario scenario, double amount) {
        using SqlConnection connection = fixture.Open();
        connection.Execute("""
            UPDATE dbo.ProductAvailability SET Amount = 0 WHERE ProductID = @ProductId AND Deleted = 0;
            UPDATE dbo.ProductAvailability SET Amount = @amount WHERE ID = @AvailabilityId;
            """, new { scenario.ProductId, scenario.AvailabilityId, amount });
    }

    private long PutStockInAnotherOrganizationStorage(CartScenario scenario, double amount) {
        using SqlConnection connection = fixture.Open();
        long storageId = connection.ExecuteScalar<long>("""
            SELECT TOP (1) s.ID
            FROM dbo.Storage s
            JOIN dbo.Organization o ON o.ID = @OrganizationId
            WHERE s.OrganizationID = o.ID AND s.ID <> o.StorageID
              AND s.Deleted = 0 AND s.ForDefective = 0
            ORDER BY s.ID;
            """, new { scenario.OrganizationId });
        long? existing = connection.ExecuteScalar<long?>(
            "SELECT TOP (1) ID FROM dbo.ProductAvailability WHERE ProductID = @ProductId AND StorageID = @storageId AND Deleted = 0;",
            new { scenario.ProductId, storageId });
        if (existing.HasValue) {
            SetAvailabilityAmount(existing.Value, amount);
            return existing.Value;
        }

        return connection.ExecuteScalar<long>("""
            INSERT INTO dbo.ProductAvailability (ProductID, StorageID, Amount, Updated)
            OUTPUT INSERTED.ID
            VALUES (@ProductId, @storageId, @amount, GETUTCDATE());
            """, new { scenario.ProductId, storageId, amount });
    }

    private void SetAvailabilityAmount(long availabilityId, double amount) {
        using SqlConnection connection = fixture.Open();
        connection.Execute("UPDATE dbo.ProductAvailability SET Amount = @amount WHERE ID = @availabilityId;", new { availabilityId, amount });
    }

    private double AvailabilityAmount(long availabilityId) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<double>("SELECT CAST(Amount AS float) FROM dbo.ProductAvailability WHERE ID = @availabilityId;", new { availabilityId });
    }

    private void SetSelected(Shopper shopper, bool selected) {
        using SqlConnection connection = fixture.Open();
        connection.Execute(
            "UPDATE dbo.Agreement SET IsSelected = @selected WHERE ID = @AgreementId;",
            new { selected, shopper.AgreementId });
    }

    private bool IsSelected(Shopper shopper) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<bool>("SELECT IsSelected FROM dbo.Agreement WHERE ID = @AgreementId;", new { shopper.AgreementId });
    }

    private long SaleClientAgreementId(long saleId) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<long>("SELECT ClientAgreementID FROM dbo.Sale WHERE ID = @saleId;", new { saleId });
    }

    private static void SetFlags(SqlConnection connection, long agreementId, bool isActive, long? pricingId, bool placeholder) {
        connection.Execute("""
            UPDATE dbo.Agreement
            SET IsActive = @isActive, IsSelected = 0, PricingID = @pricingId, IsDefaultForSyncConsignment = @placeholder
            WHERE ID = @agreementId;
            """, new { agreementId, isActive, pricingId, placeholder });
    }

    private double FreeStock(CartScenario scenario) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<double>(
            "SELECT CAST(SUM(Amount) AS float) FROM dbo.ProductAvailability WHERE ProductID = @ProductId AND Deleted = 0;",
            new { scenario.ProductId });
    }

    private double ReservedInCarts(CartScenario scenario, IEnumerable<Shopper> shoppers) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<double>("""
            SELECT CAST(ISNULL(SUM(r.Qty), 0) AS float)
            FROM dbo.ProductReservation r
            JOIN dbo.OrderItem i ON i.ID = r.OrderItemID AND i.Deleted = 0 AND i.OrderID IS NULL
            JOIN dbo.ClientShoppingCart c ON c.ID = i.ClientShoppingCartID AND c.Deleted = 0 AND c.IsOffer = 0
            JOIN dbo.ClientAgreement ca ON ca.ID = c.ClientAgreementID
            WHERE r.Deleted = 0 AND i.ProductID = @ProductId AND ca.ClientID IN @ids;
            """, new { scenario.ProductId, ids = shoppers.Select(shopper => shopper.ClientId) });
    }

    private double QuantityInCarts(CartScenario scenario, IEnumerable<Shopper> shoppers) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<double>("""
            SELECT CAST(ISNULL(SUM(i.Qty), 0) AS float)
            FROM dbo.OrderItem i
            JOIN dbo.ClientShoppingCart c ON c.ID = i.ClientShoppingCartID AND c.Deleted = 0 AND c.IsOffer = 0
            JOIN dbo.ClientAgreement ca ON ca.ID = c.ClientAgreementID
            WHERE i.Deleted = 0 AND i.OrderID IS NULL AND i.ProductID = @ProductId AND ca.ClientID IN @ids;
            """, new { scenario.ProductId, ids = shoppers.Select(shopper => shopper.ClientId) });
    }

    private DateTime? CartValidUntil(Shopper shopper) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<DateTime?>("""
            SELECT TOP (1) c.ValidUntil
            FROM dbo.ClientShoppingCart c
            JOIN dbo.ClientAgreement ca ON ca.ID = c.ClientAgreementID
            WHERE ca.ClientID = @ClientId AND c.Deleted = 0 AND c.IsOffer = 0
            ORDER BY c.ID DESC;
            """, new { shopper.ClientId });
    }

    private void AgeCart(Shopper shopper, int itemAgeDays) {
        using SqlConnection connection = fixture.Open();
        connection.Execute("""
            UPDATE c SET c.ValidUntil = DATEADD(day, -5, CAST(GETDATE() AS date))
            FROM dbo.ClientShoppingCart c
            JOIN dbo.ClientAgreement ca ON ca.ID = c.ClientAgreementID
            WHERE ca.ClientID = @ClientId AND c.Deleted = 0 AND c.IsOffer = 0;
            UPDATE i SET i.Updated = DATEADD(day, -@itemAgeDays, GETUTCDATE()), i.Created = DATEADD(day, -10, GETUTCDATE())
            FROM dbo.OrderItem i
            JOIN dbo.ClientShoppingCart c ON c.ID = i.ClientShoppingCartID AND c.Deleted = 0 AND c.IsOffer = 0
            JOIN dbo.ClientAgreement ca ON ca.ID = c.ClientAgreementID
            WHERE ca.ClientID = @ClientId AND i.Deleted = 0;
            """, new { shopper.ClientId, itemAgeDays });
    }

    private long CreateExpiredOfferWithReservation(CartScenario scenario, Shopper shopper, double qty) {
        using SqlConnection connection = fixture.Open();
        long clientAgreementId = connection.ExecuteScalar<long>(
            "SELECT TOP (1) ID FROM dbo.ClientAgreement WHERE ClientID = @ClientId AND Deleted = 0;",
            new { shopper.ClientId });
        long offerId = new ClientShoppingCartRepository(connection).AddAsOffer(new GBA.Domain.Entities.Clients.ClientShoppingCart {
            ValidUntil = DateTime.Now.Date.AddDays(-10),
            ClientAgreementId = clientAgreementId,
            Number = "EXPIRY-TEST"
        });
        long itemId = new OrderItemRepository(connection).Add(new OrderItem {
            ClientShoppingCartId = offerId,
            ProductId = scenario.ProductId,
            Qty = qty,
            OverLordQty = qty,
            OrderedQty = qty,
            PricePerItem = 1m,
            ExchangeRateAmount = 1m,
            IsValidForCurrentSale = true
        });
        connection.Execute("""
            UPDATE dbo.OrderItem SET Updated = DATEADD(day, -10, GETUTCDATE()) WHERE ID = @itemId;
            UPDATE dbo.ProductAvailability SET Amount = Amount - @qty WHERE ID = @AvailabilityId;
            INSERT INTO dbo.ProductReservation (OrderItemID, ProductAvailabilityID, Qty, IsReSaleReservation, Updated)
            VALUES (@itemId, @AvailabilityId, @qty, 0, GETUTCDATE());
            """, new { itemId, qty, scenario.AvailabilityId });
        return offerId;
    }

    private bool CartDeleted(long cartId) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<bool>("SELECT Deleted FROM dbo.ClientShoppingCart WHERE ID = @cartId;", new { cartId });
    }

    private double ReservedInCart(long cartId) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<double>("""
            SELECT CAST(ISNULL(SUM(r.Qty), 0) AS float)
            FROM dbo.ProductReservation r
            JOIN dbo.OrderItem i ON i.ID = r.OrderItemID
            WHERE i.ClientShoppingCartID = @cartId AND r.Deleted = 0 AND i.Deleted = 0;
            """, new { cartId });
    }

    private void DeleteOffer(long offerId) {
        using SqlConnection connection = fixture.Open();
        connection.Execute("""
            UPDATE pa SET pa.Amount = pa.Amount + r.Qty
            FROM dbo.ProductAvailability pa
            JOIN dbo.ProductReservation r ON r.ProductAvailabilityID = pa.ID AND r.Deleted = 0
            JOIN dbo.OrderItem i ON i.ID = r.OrderItemID AND i.ClientShoppingCartID = @offerId;
            UPDATE r SET r.Deleted = 1
            FROM dbo.ProductReservation r
            JOIN dbo.OrderItem i ON i.ID = r.OrderItemID AND i.ClientShoppingCartID = @offerId;
            UPDATE dbo.OrderItem SET Deleted = 1 WHERE ClientShoppingCartID = @offerId;
            UPDATE dbo.ClientShoppingCart SET Deleted = 1 WHERE ID = @offerId;
            """, new { offerId });
    }

    private static OrderItem NewLine(Guid productNetUid, double qty) {
        return new OrderItem { Product = new Product { NetUid = productNetUid }, Qty = qty, OverLordQty = qty };
    }

    private ClientShoppingCartService NewCartService() {
        return new ClientShoppingCartService(
            new ClientRepositoriesFactory(),
            new SaleRepositoriesFactory(),
            new ProductRepositoriesFactory(),
            new StorageRepositoryFactory(),
            new PricingRepositoriesFactory(),
            new ExchangeRateRepositoriesFactory(),
            new CurrencyRepositoriesFactory(),
            fixture.ConnectionFactory,
            new AgreementRepositoriesFactory(),
            new RecordingCrmHandler().Factory(),
            new SearchReindexSignal());
    }

    private OrderService NewOrderService() {
        RecordingCrmHandler crm = new();
        return new OrderService(
            new SaleRepositoriesFactory(),
            new ClientRepositoriesFactory(),
            new UserRepositoriesFactory(),
            new ProductRepositoriesFactory(),
            new AgreementRepositoriesFactory(),
            new StorageRepositoryFactory(),
            new ExchangeRateRepositoriesFactory(),
            new CurrencyRepositoriesFactory(),
            new DeliveryRepositoriesFactory(),
            new RetailClientRepositoriesFactory(),
            fixture.ConnectionFactory,
            new PaymentLinkService(),
            crm.Factory(),
            new ClientResourceAccessService(
                fixture.ConnectionFactory,
                new ClientRepositoriesFactory(),
                new DeliveryRepositoriesFactory(),
                new SaleRepositoriesFactory()),
            new SearchReindexSignal(),
            new CheckoutExecutor(fixture.ConnectionFactory, new SqlCheckoutLedger()));
    }

    private ProductService NewProductService() {
        return new ProductService(
            new ClientRepositoriesFactory(),
            new ProductRepositoriesFactory(),
            new PricingRepositoriesFactory(),
            new ExchangeRateRepositoriesFactory(),
            new CurrencyRepositoriesFactory(),
            new OrganizationRepositoriesFactory(),
            new StorageRepositoryFactory(),
            fixture.ConnectionFactory,
            new AgreementRepositoriesFactory());
    }

    private sealed record CartScenario(
        long OrganizationId,
        long ProductId,
        Guid ProductNetUid,
        long AvailabilityId,
        Guid HiddenProductNetUid,
        List<Shopper> Shoppers);

    private sealed class Shopper {
        public long ClientId { get; init; }
        public Guid ClientNetUid { get; init; }
        public long AgreementId { get; init; }
        public long ClientAgreementId { get; init; }
    }

    private sealed class AgreementFlags {
        public long AgreementId { get; init; }
        public long ClientAgreementId { get; init; }
        public bool IsActive { get; init; }
        public bool IsSelected { get; init; }
        public long? PricingId { get; init; }
        public bool IsDefaultForSyncConsignment { get; init; }
    }
}
