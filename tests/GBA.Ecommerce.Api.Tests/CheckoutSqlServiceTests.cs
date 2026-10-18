using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using GBA.Common.Search;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Agreements;
using GBA.Domain.Repositories.Clients;
using GBA.Domain.Repositories.Currencies;
using GBA.Domain.Repositories.Delivery;
using GBA.Domain.Repositories.ExchangeRates;
using GBA.Domain.Repositories.Pricings;
using GBA.Domain.Repositories.Products;
using GBA.Domain.Repositories.Sales;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages;
using GBA.Domain.Repositories.Users;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.Infrastructure.Checkout;
using GBA.Services.Services.Clients;
using GBA.Services.Services.Messengers.Contracts;
using GBA.Services.Services.Offers;
using GBA.Services.Services.Orders;
using Microsoft.Data.SqlClient;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

[Collection(CheckoutSqlCollection.Name)]
public sealed class CheckoutSqlServiceTests(CheckoutSqlFixture fixture) {
    private static readonly object ScenarioGate = new();
    private static CheckoutScenario? _scenario;
    private static TwoCartScenario? _twoCartScenario;
    private static bool _twoCartScenarioSearched;

    private readonly RecordingCrmHandler _crm = new();

    [Fact]
    public async Task Invoice_checkout_moves_the_cart_into_one_order_once_and_replays_the_same_sale() {
        if (!Ready(out CheckoutScenario scenario)) return;

        OrderItem item = await AddFreshCartItem(scenario, 0);
        CartItemState before = ReadItem(item.NetUid);
        CheckoutAttempt attempt = NewAttempt();
        Watermark watermark = CurrentWatermark();
        (Guid recipientNetUid, Guid addressNetUid) = CreateDeliveryRecipient(scenario);

        CheckoutResult first = await NewOrderService().GenerateNewSaleWithInvoice(
            InvoiceFor(item, recipientNetUid, addressNetUid), scenario.ClientNetUid, false, scenario.WithVat, attempt);

        Assert.False(first.IsReplay);
        Assert.Equal((recipientNetUid, addressNetUid), SaleDelivery(first.Sale.Id));
        CartItemState moved = ReadItem(item.NetUid);
        Assert.Null(moved.ClientShoppingCartId);
        Assert.False(moved.Deleted);
        Assert.Equal(SaleOrderId(first.Sale.Id), moved.OrderId);
        Assert.Equal(before.ReservedQty, moved.ReservedQty);
        Assert.Equal(1, OrdersCreatedSince(scenario, watermark));
        Assert.True(ReceiptCompleted(attempt.IdempotencyKey));
        Assert.True(await _crm.WaitForAsync("sales/sync/new", 1, TimeSpan.FromSeconds(5)));
        Assert.True(await _crm.WaitForAsync("sales/update/ecommerce", 1, TimeSpan.FromSeconds(5)));

        CheckoutResult replay = await NewOrderService().GenerateNewSaleWithInvoice(
            InvoiceFor(item, recipientNetUid, addressNetUid), scenario.ClientNetUid, false, scenario.WithVat, attempt);

        Assert.True(replay.IsReplay);
        Assert.Equal(first.Sale.Id, replay.Sale.Id);
        Assert.Equal(first.Sale.NetUid, replay.Sale.NetUid);
        Assert.Equal(first.Sale.SaleNumber?.Value, replay.Sale.SaleNumber?.Value);
        Assert.Equal(1, OrdersCreatedSince(scenario, watermark));
        await Task.Delay(500);
        Assert.Equal(1, _crm.Count("sales/sync/new"));

        using SqlConnection connection = fixture.Open();
        Assert.False(new OrderItemRepository(connection).RemoveFromShoppingCart(item.NetUid, before.ClientShoppingCartId!.Value));
        Assert.False(ReadItem(item.NetUid).Deleted);
    }

    [Fact]
    public async Task A_failure_after_the_sale_insert_leaves_the_cart_and_stock_untouched() {
        if (!Ready(out CheckoutScenario scenario)) return;

        OrderItem item = await AddFreshCartItem(scenario, 1);
        CartItemState before = ReadItem(item.NetUid);
        CheckoutAttempt attempt = NewAttempt();
        Watermark watermark = CurrentWatermark();
        double availableBefore = AvailableStock(item.ProductId, scenario.OrganizationId);

        fixture.RemoveCrmConfiguration();
        try {
            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                NewOrderService().GenerateNewSaleWithInvoice(
                    InvoiceFor(item), scenario.ClientNetUid, false, scenario.WithVat, attempt));
            Assert.Equal("CRM endpoint is not configured.", failure.Message);
        } finally {
            fixture.WriteCrmConfiguration();
        }

        CartItemState after = ReadItem(item.NetUid);
        Assert.Equal(before.ClientShoppingCartId, after.ClientShoppingCartId);
        Assert.Null(after.OrderId);
        Assert.False(after.Deleted);
        Assert.Equal(before.ReservedQty, after.ReservedQty);
        Assert.Equal(availableBefore, AvailableStock(item.ProductId, scenario.OrganizationId));
        Assert.Equal(0, OrdersCreatedSince(scenario, watermark));
        Assert.Equal(0, SalesCreatedSince(scenario, watermark));
        Assert.False(ReceiptExists(attempt.IdempotencyKey));
        await Task.Delay(500);
        Assert.Empty(_crm.Paths);

        CheckoutResult retried = await NewOrderService().GenerateNewSaleWithInvoice(
            InvoiceFor(item), scenario.ClientNetUid, false, scenario.WithVat, attempt);

        Assert.False(retried.IsReplay);
        Assert.Equal(SaleOrderId(retried.Sale.Id), ReadItem(item.NetUid).OrderId);
    }

    [Fact]
    public async Task Simultaneous_invoice_submissions_with_one_key_create_one_order() {
        if (!Ready(out CheckoutScenario scenario)) return;

        OrderItem item = await AddFreshCartItem(scenario, 2);
        CheckoutAttempt attempt = NewAttempt();
        Watermark watermark = CurrentWatermark();

        CheckoutResult[] results = await SettleSameKeyRace(Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
            NewOrderService().GenerateNewSaleWithInvoice(
                InvoiceFor(item), scenario.ClientNetUid, false, scenario.WithVat, attempt))));
        CheckoutResult replay = await NewOrderService().GenerateNewSaleWithInvoice(
            InvoiceFor(item), scenario.ClientNetUid, false, scenario.WithVat, attempt);

        Assert.Single(results, result => !result.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.All(results, result => Assert.Equal(replay.Sale.NetUid, result.Sale.NetUid));
        Assert.Equal(1, OrdersCreatedSince(scenario, watermark));
        Assert.Equal(SaleOrderId(replay.Sale.Id), ReadItem(item.NetUid).OrderId);
        Assert.True(await _crm.WaitForAsync("sales/sync/new", 1, TimeSpan.FromSeconds(5)));
        await Task.Delay(500);
        Assert.Equal(1, _crm.Count("sales/sync/new"));
    }

    [Fact]
    public async Task Two_tabs_checking_out_the_same_cart_with_different_keys_order_the_items_once() {
        if (!Ready(out CheckoutScenario scenario)) return;

        OrderItem item = await AddFreshCartItem(scenario, 0);
        Watermark watermark = CurrentWatermark();

        Task<CheckoutResult>[] attempts = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
            NewOrderService().GenerateNewSaleWithInvoice(
                InvoiceFor(item), scenario.ClientNetUid, false, scenario.WithVat, NewAttempt()))).ToArray();

        await Task.WhenAll(attempts.Select(task => task.ContinueWith(_ => { })));

        Assert.Equal(1, attempts.Count(task => task.IsCompletedSuccessfully));
        Exception rejected = attempts.Single(task => task.IsFaulted).Exception!.InnerException!;
        Assert.True(rejected is CheckoutConflictException or ArgumentException, rejected.ToString());
        Assert.Equal(1, OrdersCreatedSince(scenario, watermark));
        Assert.Equal(1, SalesCreatedSince(scenario, watermark));
        Assert.NotNull(ReadItem(item.NetUid).OrderId);
    }

    [Fact]
    public async Task Offer_acceptance_is_atomic_and_idempotent() {
        if (!Ready(out CheckoutScenario scenario)) return;

        long productId = OfferScopeProduct(scenario, 0);
        (Guid offerNetUid, Guid offerItemNetUid) = CreateOffer(scenario, productId, 2);
        double availableBefore = OfferScopeStock(productId, scenario);
        CheckoutAttempt attempt = NewAttempt();
        Watermark watermark = CurrentWatermark();
        SeedLegacyOfferSaleNumber(scenario);

        CheckoutResult[] results = await SettleSameKeyRace(Enumerable.Range(0, 3).Select(_ => Task.Run(() =>
            NewOfferService().GenerateNewOrderAndSaleFromOffer(
                OfferSelection(offerNetUid, offerItemNetUid, 2), scenario.ClientNetUid, false, attempt))));
        CheckoutResult replay = await NewOfferService().GenerateNewOrderAndSaleFromOffer(
            OfferSelection(offerNetUid, offerItemNetUid, 2), scenario.ClientNetUid, false, attempt);

        Assert.Single(results, result => !result.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.All(results, result => Assert.Equal(replay.Sale.NetUid, result.Sale.NetUid));
        Assert.Equal(1, OrdersCreatedSince(scenario, watermark));
        Assert.True(OfferProcessed(offerNetUid));
        double reserved = ReservedForOrder(SaleOrderId(replay.Sale.Id));
        Assert.Equal(2, reserved);
        Assert.Equal(availableBefore - 2, OfferScopeStock(productId, scenario));
        Assert.True(await _crm.WaitForAsync("sales/sync/new", 1, TimeSpan.FromSeconds(5)));
        await Task.Delay(500);
        Assert.Equal(1, _crm.Count("sales/sync/new"));
    }

    [Fact]
    public async Task Offer_acceptance_failing_after_the_stock_writes_leaves_the_offer_cart_and_stock_untouched() {
        if (!Ready(out CheckoutScenario scenario)) return;

        OrderItem cartItem = await AddFreshCartItem(scenario, 2);
        CartItemState cartBefore = ReadItem(cartItem.NetUid);
        long productId = OfferScopeProduct(scenario, 1);
        (Guid offerNetUid, Guid offerItemNetUid) = CreateOffer(scenario, productId, 1);
        double availableBefore = OfferScopeStock(productId, scenario);
        CheckoutAttempt attempt = NewAttempt();
        Watermark watermark = CurrentWatermark();
        SeedLegacyOfferSaleNumber(scenario);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NewOfferService(FailingSaleInsertFactory()).GenerateNewOrderAndSaleFromOffer(
                OfferSelection(offerNetUid, offerItemNetUid, 1), scenario.ClientNetUid, true, attempt));

        Assert.False(OfferProcessed(offerNetUid));
        Assert.Equal(availableBefore, OfferScopeStock(productId, scenario));
        Assert.Equal(0, OrdersCreatedSince(scenario, watermark));
        Assert.Equal(0, SalesCreatedSince(scenario, watermark));
        Assert.False(ReceiptExists(attempt.IdempotencyKey));
        CartItemState cartAfter = ReadItem(cartItem.NetUid);
        Assert.Equal(cartBefore.ClientShoppingCartId, cartAfter.ClientShoppingCartId);
        Assert.Null(cartAfter.OrderId);
        await Task.Delay(500);
        Assert.Empty(_crm.Paths);

        SeedLegacyOfferSaleNumber(scenario);
        CheckoutResult retried = await NewOfferService().GenerateNewOrderAndSaleFromOffer(
            OfferSelection(offerNetUid, offerItemNetUid, 1), scenario.ClientNetUid, true, attempt);

        Assert.False(retried.IsReplay);
        Assert.True(OfferProcessed(offerNetUid));
        Assert.Equal(availableBefore - 1, OfferScopeStock(productId, scenario));
        Assert.Equal(SaleOrderId(retried.Sale.Id), ReadItem(cartItem.NetUid).OrderId);
    }

    [Fact]
    public async Task Offer_acceptance_moves_the_current_cart_items_into_the_same_order() {
        if (!Ready(out CheckoutScenario scenario)) return;

        OrderItem cartItem = await AddFreshCartItem(scenario, 0);
        (Guid offerNetUid, Guid offerItemNetUid) = CreateOffer(scenario, OfferScopeProduct(scenario, 2), 1);
        SeedLegacyOfferSaleNumber(scenario);

        CheckoutResult result = await NewOfferService().GenerateNewOrderAndSaleFromOffer(
            OfferSelection(offerNetUid, offerItemNetUid, 1), scenario.ClientNetUid, true, NewAttempt());

        CartItemState moved = ReadItem(cartItem.NetUid);
        Assert.Equal(SaleOrderId(result.Sale.Id), moved.OrderId);
        Assert.Null(moved.ClientShoppingCartId);
    }

    [Fact]
    public async Task Different_carts_of_one_client_check_out_at_the_same_time_without_waiting_for_each_other() {
        if (!Ready(out _)) return;

        TwoCartScenario? twoCarts = TwoCarts();
        if (twoCarts == null) return;

        await PrepareCart(twoCarts.NonVat, twoCarts.NonVatProductNetUid);
        await PrepareCart(twoCarts.Vat, twoCarts.VatProductNetUid);

        using Barrier bothInsideTheirTransactions = new(2);
        Mock<IUserRepositoriesFactory> users = new();
        users
            .Setup(factory => factory.NewUserRepository(It.IsAny<IDbConnection>()))
            .Returns<IDbConnection>(connection => {
                if (!bothInsideTheirTransactions.SignalAndWait(TimeSpan.FromSeconds(20)))
                    throw new TimeoutException("The two cart checkouts blocked each other.");
                return new UserRepository(connection);
            });

        CheckoutResult[] results = await Task.WhenAll(
            Task.Run(() => NewOrderService(users.Object).GenerateNewOrderAndSaleFromClientShoppingCart(
                twoCarts.NonVat.ClientNetUid, false, NewAttempt())),
            Task.Run(() => NewOrderService(users.Object).GenerateNewOrderAndSaleFromClientShoppingCart(
                twoCarts.Vat.ClientNetUid, true, NewAttempt())));

        Assert.All(results, result => Assert.False(result.IsReplay));
        Assert.NotEqual(results[0].Sale.OrderId, results[1].Sale.OrderId);
    }

    private static async Task<CheckoutResult[]> SettleSameKeyRace(IEnumerable<Task<CheckoutResult>> submissions) {
        Task<CheckoutResult>[] tasks = submissions.ToArray();
        await Task.WhenAll(tasks.Select(task => task.ContinueWith(_ => { })));

        foreach (Task<CheckoutResult> task in tasks.Where(task => task.IsFaulted))
            Assert.IsType<CheckoutConflictException>(task.Exception!.InnerException);

        return tasks.Where(task => task.IsCompletedSuccessfully).Select(task => task.Result).ToArray();
    }

    private bool Ready(out CheckoutScenario scenario) {
        scenario = null!;
        if (!fixture.IsConfigured || !fixture.HasConcordSchema) return false;

        UseUkrainianCulture();
        lock (ScenarioGate) {
            _scenario ??= DiscoverScenario();
        }

        scenario = _scenario;
        return true;
    }

    private TwoCartScenario? TwoCarts() {
        lock (ScenarioGate) {
            if (!_twoCartScenarioSearched) {
                _twoCartScenario = DiscoverTwoCartScenario();
                _twoCartScenarioSearched = true;
            }
        }

        return _twoCartScenario;
    }

    private static void UseUkrainianCulture() {
        CultureInfo culture = new("uk-UA");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private CheckoutScenario DiscoverScenario() {
        foreach (CheckoutScenario candidate in AgreementCandidates(vatMode: null, forOffers: true)) {
            List<Guid> products = PurchasableProducts(candidate, 3);
            if (products.Count == 3) return candidate with { ProductNetUids = products };
        }

        throw new InvalidOperationException("No client agreement with three purchasable products was found in the checkout test database.");
    }

    private TwoCartScenario? DiscoverTwoCartScenario() {
        using SqlConnection connection = fixture.Open();
        List<long> clients = connection.Query<long>("""
            SELECT c.ID
            FROM dbo.Client c
            JOIN dbo.ClientAgreement ca ON ca.ClientID = c.ID AND ca.Deleted = 0
            JOIN dbo.Agreement a ON a.ID = ca.AgreementID AND a.Deleted = 0 AND a.IsActive = 1 AND a.ForReSale = 0 AND a.CurrencyID IS NOT NULL
            JOIN dbo.Organization o ON o.ID = a.OrganizationID AND o.Deleted = 0
            WHERE c.Deleted = 0
              AND NOT EXISTS (SELECT 1 FROM dbo.ClientSubClient l WHERE l.SubClientID = c.ID AND l.Deleted = 0)
            GROUP BY c.ID
            HAVING SUM(CASE WHEN a.WithVATAccounting = 0 THEN 1 ELSE 0 END) > 0
               AND SUM(CASE WHEN a.WithVATAccounting = 1 THEN 1 ELSE 0 END) > 0
            ORDER BY COUNT(*) DESC, c.ID;
            """).Take(10).ToList();

        foreach (long clientId in clients) {
            CheckoutScenario? nonVat = AgreementCandidates(false, clientId)
                .Select(candidate => candidate with { ProductNetUids = PurchasableProducts(candidate, 1) })
                .FirstOrDefault(candidate => candidate.ProductNetUids.Count == 1);
            CheckoutScenario? vat = AgreementCandidates(true, clientId)
                .Select(candidate => candidate with { ProductNetUids = PurchasableProducts(candidate, 1) })
                .FirstOrDefault(candidate => candidate.ProductNetUids.Count == 1);

            if (nonVat != null && vat != null)
                return new TwoCartScenario(nonVat, nonVat.ProductNetUids[0], vat, vat.ProductNetUids[0]);
        }

        return null;
    }

    private IEnumerable<CheckoutScenario> AgreementCandidates(bool? vatMode, long? clientId = null, bool forOffers = false) {
        using SqlConnection connection = fixture.Open();
        return connection.Query<CheckoutScenario>("""
            SELECT TOP (30)
                c.ID AS ClientId,
                c.NetUID AS ClientNetUid,
                a.ID AS AgreementId,
                ca.ID AS ClientAgreementId,
                a.OrganizationID AS OrganizationId,
                a.WithVATAccounting AS WithVat
            FROM dbo.Client c
            JOIN dbo.ClientAgreement ca ON ca.ClientID = c.ID AND ca.Deleted = 0
            JOIN dbo.Agreement a ON a.ID = ca.AgreementID AND a.Deleted = 0 AND a.IsActive = 1 AND a.ForReSale = 0 AND a.CurrencyID IS NOT NULL
            JOIN dbo.Organization o ON o.ID = a.OrganizationID AND o.Deleted = 0
            WHERE c.Deleted = 0
              AND (@clientId IS NULL OR c.ID = @clientId)
              AND (@vatMode IS NULL OR a.WithVATAccounting = @vatMode)
              AND NOT EXISTS (SELECT 1 FROM dbo.ClientSubClient l WHERE l.SubClientID = c.ID AND l.Deleted = 0)
              AND (@clientId IS NOT NULL OR EXISTS (
                    SELECT 1 FROM dbo.Sale s JOIN dbo.[Order] so ON so.ID = s.OrderID
                    WHERE s.ClientAgreementID = ca.ID AND so.OrderSource = 0 AND s.Deleted = 0))
              AND (@forOffers = 0 OR (
                    (SELECT COUNT(*)
                     FROM dbo.ClientAgreement sameMode
                     JOIN dbo.Agreement sameModeAgreement ON sameModeAgreement.ID = sameMode.AgreementID
                     WHERE sameMode.ClientID = c.ID AND sameMode.Deleted = 0
                       AND sameModeAgreement.WithVATAccounting = a.WithVATAccounting) = 1
                    AND EXISTS (
                        SELECT 1
                        FROM dbo.ProductAvailability pa
                        JOIN dbo.Storage st ON st.ID = pa.StorageID
                        WHERE st.OrganizationID = a.OrganizationID AND st.ForDefective = 0
                          AND st.ForVatProducts = a.WithVATAccounting
                          AND pa.Deleted = 0 AND pa.Amount >= 20)))
            ORDER BY ca.ID DESC;
            """, new { clientId, vatMode, forOffers }).ToList();
    }

    private List<Guid> PurchasableProducts(CheckoutScenario candidate, int wanted) {
        SelectAgreement(candidate);

        List<Guid> candidates;
        using (SqlConnection connection = fixture.Open()) {
            candidates = connection.Query<Guid>("""
                SELECT TOP (25) p.NetUID
                FROM dbo.ProductAvailability pa
                JOIN dbo.Storage s ON s.ID = pa.StorageID
                JOIN dbo.Product p ON p.ID = pa.ProductID
                WHERE pa.Deleted = 0 AND pa.Amount >= 20
                  AND s.Deleted = 0 AND s.ForDefective = 0 AND s.Locale = N'uk'
                  AND (s.OrganizationID = @OrganizationId OR EXISTS (
                        SELECT 1 FROM dbo.Organization ao
                        WHERE ao.ID = @OrganizationId AND ao.Deleted = 0 AND ao.StorageID = s.ID))
                  AND p.Deleted = 0 AND p.IsForWeb = 1
                ORDER BY pa.Amount DESC, p.ID;
                """, new { candidate.OrganizationId }).Distinct().ToList();
        }

        ClientShoppingCartService cartService = NewCartService();
        List<Guid> purchasable = new();
        cartService.DeleteAllItemsFromShoppingCartByClientNetId(candidate.ClientNetUid, candidate.WithVat).GetAwaiter().GetResult();

        foreach (Guid productNetUid in candidates) {
            try {
                cartService.Add(NewCartLine(productNetUid), candidate.ClientNetUid, candidate.WithVat).GetAwaiter().GetResult();
                purchasable.Add(productNetUid);
            } catch (Exception) {
            }

            if (purchasable.Count == wanted) break;
        }

        cartService.DeleteAllItemsFromShoppingCartByClientNetId(candidate.ClientNetUid, candidate.WithVat).GetAwaiter().GetResult();
        return purchasable;
    }

    private void SelectAgreement(CheckoutScenario scenario) {
        using SqlConnection connection = fixture.Open();
        connection.Execute("""
            UPDATE a
            SET a.IsSelected = CASE WHEN a.ID = @AgreementId THEN 1 ELSE 0 END
            FROM dbo.Agreement a
            JOIN dbo.ClientAgreement ca ON ca.AgreementID = a.ID
            WHERE ca.ClientID = @ClientId;
            """, new { scenario.AgreementId, scenario.ClientId });
    }

    private async Task PrepareCart(CheckoutScenario scenario, Guid productNetUid) {
        SelectAgreement(scenario);
        ClientShoppingCartService cartService = NewCartService();
        await cartService.DeleteAllItemsFromShoppingCartByClientNetId(scenario.ClientNetUid, scenario.WithVat);
        await cartService.Add(NewCartLine(productNetUid), scenario.ClientNetUid, scenario.WithVat);
    }

    private async Task<OrderItem> AddFreshCartItem(CheckoutScenario scenario, int productIndex) {
        SelectAgreement(scenario);
        ClientShoppingCartService cartService = NewCartService();
        await cartService.DeleteAllItemsFromShoppingCartByClientNetId(scenario.ClientNetUid, scenario.WithVat);
        OrderItem item = await cartService.Add(
            NewCartLine(scenario.ProductNetUids[productIndex]),
            scenario.ClientNetUid,
            scenario.WithVat);
        Assert.NotEqual(Guid.Empty, item.NetUid);
        return item;
    }

    private static OrderItem NewCartLine(Guid productNetUid) {
        return new OrderItem { Product = new Product { NetUid = productNetUid }, Qty = 1, OverLordQty = 1 };
    }

    private static Sale InvoiceFor(OrderItem item, Guid? recipientNetUid = null, Guid? addressNetUid = null) {
        Sale sale = new() { Comment = "checkout atomicity test", Order = new Order() };
        sale.Order.OrderItems.Add(new OrderItem { NetUid = item.NetUid, Qty = item.Qty });
        if (recipientNetUid.HasValue) sale.DeliveryRecipient = new DeliveryRecipient { NetUid = recipientNetUid.Value };
        if (addressNetUid.HasValue) sale.DeliveryRecipientAddress = new DeliveryRecipientAddress { NetUid = addressNetUid.Value };
        return sale;
    }

    private (Guid RecipientNetUid, Guid AddressNetUid) CreateDeliveryRecipient(CheckoutScenario scenario) {
        using SqlConnection connection = fixture.Open();
        long recipientId = new DeliveryRecipientRepository(connection).Add(new DeliveryRecipient {
            ClientId = scenario.ClientId,
            FullName = "Checkout Atomicity Recipient",
            MobilePhone = "+380000000000"
        });
        new DeliveryRecipientAddressRepository(connection).Add(new DeliveryRecipientAddress {
            DeliveryRecipientId = recipientId,
            Value = "Test street 1",
            City = "Kyiv",
            Department = "1"
        });

        return connection.QuerySingle<(Guid, Guid)>(
            "SELECT r.NetUID, a.NetUID FROM dbo.DeliveryRecipient r JOIN dbo.DeliveryRecipientAddress a ON a.DeliveryRecipientID = r.ID WHERE r.ID = @recipientId;",
            new { recipientId });
    }

    private (Guid RecipientNetUid, Guid AddressNetUid) SaleDelivery(long saleId) {
        using SqlConnection connection = fixture.Open();
        return connection.QuerySingle<(Guid, Guid)>(
            "SELECT r.NetUID, a.NetUID FROM dbo.Sale s JOIN dbo.DeliveryRecipient r ON r.ID = s.DeliveryRecipientID JOIN dbo.DeliveryRecipientAddress a ON a.ID = s.DeliveryRecipientAddressID WHERE s.ID = @saleId;",
            new { saleId });
    }

    private static ClientShoppingCart OfferSelection(Guid offerNetUid, Guid offerItemNetUid, double qty) {
        ClientShoppingCart selection = new() { NetUid = offerNetUid };
        selection.OrderItems.Add(new OrderItem { NetUid = offerItemNetUid, Qty = qty });
        return selection;
    }

    private (Guid OfferNetUid, Guid OfferItemNetUid) CreateOffer(CheckoutScenario scenario, long productId, double qty) {
        using SqlConnection connection = fixture.Open();
        long offerId = new ClientShoppingCartRepository(connection).AddAsOffer(new ClientShoppingCart {
            ValidUntil = DateTime.Now.Date.AddDays(3),
            ClientAgreementId = scenario.ClientAgreementId,
            Number = "ATOMIC-TEST",
            IsVatCart = scenario.WithVat
        });
        new OrderItemRepository(connection).Add(new OrderItem {
            ClientShoppingCartId = offerId,
            ProductId = productId,
            Qty = qty,
            OverLordQty = qty,
            OrderedQty = qty,
            PricePerItem = 1.25m,
            ExchangeRateAmount = 1m,
            IsValidForCurrentSale = true
        });

        return connection.QuerySingle<(Guid, Guid)>("""
            SELECT c.NetUID, i.NetUID
            FROM dbo.ClientShoppingCart c
            JOIN dbo.OrderItem i ON i.ClientShoppingCartID = c.ID
            WHERE c.ID = @offerId;
            """, new { offerId });
    }

    private void SeedLegacyOfferSaleNumber(CheckoutScenario scenario) {
        using SqlConnection connection = fixture.Open();
        string code = connection.ExecuteScalar<string?>(
            "SELECT ISNULL(Code, N'') FROM dbo.Organization WHERE ID = @OrganizationId;",
            new { scenario.OrganizationId }) ?? string.Empty;
        new SaleNumberRepository(connection).Add(new SaleNumber {
            OrganizationId = scenario.OrganizationId,
            Value = code + Random.Shared.Next(1_000_000, 1_900_000_000).ToString("D10", CultureInfo.InvariantCulture)
        });
    }

    private OrderService NewOrderService(IUserRepositoriesFactory? users = null) {
        return new OrderService(
            new SaleRepositoriesFactory(),
            new ClientRepositoriesFactory(),
            users ?? new UserRepositoriesFactory(),
            new ProductRepositoriesFactory(),
            new AgreementRepositoriesFactory(),
            new StorageRepositoryFactory(),
            new ExchangeRateRepositoriesFactory(),
            new CurrencyRepositoriesFactory(),
            new DeliveryRepositoriesFactory(),
            new RetailClientRepositoriesFactory(),
            fixture.ConnectionFactory,
            Mock.Of<IPaymentLinkService>(),
            _crm.Factory(),
            NewAccessService(),
            new SearchReindexSignal(),
            new CheckoutExecutor(fixture.ConnectionFactory, new SqlCheckoutLedger()));
    }

    private OfferService NewOfferService(ISaleRepositoriesFactory? sales = null) {
        return new OfferService(
            new ClientRepositoriesFactory(),
            sales ?? new SaleRepositoriesFactory(),
            new ProductRepositoriesFactory(),
            new UserRepositoriesFactory(),
            fixture.ConnectionFactory,
            _crm.Factory(),
            NewAccessService(),
            new CheckoutExecutor(fixture.ConnectionFactory, new SqlCheckoutLedger()));
    }

    private static ISaleRepositoriesFactory FailingSaleInsertFactory() {
        SaleRepositoriesFactory real = new();
        Mock<ISaleRepositoriesFactory> factory = new(MockBehavior.Strict);
        factory.Setup(candidate => candidate.NewOrderRepository(It.IsAny<IDbConnection>()))
            .Returns<IDbConnection>(real.NewOrderRepository);
        factory.Setup(candidate => candidate.NewOrderItemRepository(It.IsAny<IDbConnection>()))
            .Returns<IDbConnection>(real.NewOrderItemRepository);
        factory.Setup(candidate => candidate.NewBaseLifeCycleStatusRepository(It.IsAny<IDbConnection>()))
            .Returns<IDbConnection>(real.NewBaseLifeCycleStatusRepository);
        factory.Setup(candidate => candidate.NewBaseSalePaymentStatusRepository(It.IsAny<IDbConnection>()))
            .Returns<IDbConnection>(real.NewBaseSalePaymentStatusRepository);
        factory.Setup(candidate => candidate.NewSaleNumberRepository(It.IsAny<IDbConnection>()))
            .Returns<IDbConnection>(real.NewSaleNumberRepository);
        factory.Setup(candidate => candidate.NewSaleRepository(It.IsAny<IDbConnection>()))
            .Throws(new InvalidOperationException("Simulated failure before the sale insert."));
        return factory.Object;
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

    private ClientResourceAccessService NewAccessService() {
        return new ClientResourceAccessService(
            fixture.ConnectionFactory,
            new ClientRepositoriesFactory(),
            new DeliveryRepositoriesFactory(),
            new SaleRepositoriesFactory());
    }

    private static CheckoutAttempt NewAttempt() {
        return new CheckoutAttempt(Guid.NewGuid(), CheckoutFingerprint.Compute(Guid.NewGuid().ToString()));
    }

    private Watermark CurrentWatermark() {
        using SqlConnection connection = fixture.Open();
        return connection.QuerySingle<Watermark>(
            "SELECT ISNULL((SELECT MAX(ID) FROM dbo.[Order]), 0) AS OrderId, ISNULL((SELECT MAX(ID) FROM dbo.Sale), 0) AS SaleId;");
    }

    private CartItemState ReadItem(Guid netUid) {
        using SqlConnection connection = fixture.Open();
        return connection.QuerySingle<CartItemState>("""
            SELECT
                i.ID AS Id,
                i.ClientShoppingCartID AS ClientShoppingCartId,
                i.OrderID AS OrderId,
                i.Deleted,
                CAST(ISNULL((SELECT SUM(r.Qty) FROM dbo.ProductReservation r WHERE r.OrderItemID = i.ID AND r.Deleted = 0), 0) AS float) AS ReservedQty
            FROM dbo.OrderItem i
            WHERE i.NetUID = @netUid;
            """, new { netUid });
    }

    private long OfferScopeProduct(CheckoutScenario scenario, int index) {
        using SqlConnection connection = fixture.Open();
        return connection.Query<long>("""
            SELECT TOP (10) pa.ProductID
            FROM dbo.ProductAvailability pa
            JOIN dbo.Storage s ON s.ID = pa.StorageID
            JOIN dbo.Product p ON p.ID = pa.ProductID AND p.Deleted = 0
            WHERE pa.Deleted = 0 AND pa.Amount >= 20
              AND s.OrganizationID = @OrganizationId AND s.ForDefective = 0 AND s.ForVatProducts = @WithVat
            GROUP BY pa.ProductID
            ORDER BY MAX(pa.Amount) DESC, pa.ProductID;
            """, new { scenario.OrganizationId, scenario.WithVat }).ElementAt(index);
    }

    private double OfferScopeStock(long productId, CheckoutScenario scenario) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<double>("""
            SELECT CAST(ISNULL(SUM(pa.Amount), 0) AS float)
            FROM dbo.ProductAvailability pa
            JOIN dbo.Storage s ON s.ID = pa.StorageID
            WHERE pa.ProductID = @productId AND pa.Deleted = 0
              AND s.OrganizationID = @OrganizationId AND s.ForDefective = 0 AND s.ForVatProducts = @WithVat;
            """, new { productId, scenario.OrganizationId, scenario.WithVat });
    }

    private double AvailableStock(long productId, long organizationId) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<double>("""
            SELECT CAST(ISNULL(SUM(pa.Amount), 0) AS float)
            FROM dbo.ProductAvailability pa
            JOIN dbo.Storage s ON s.ID = pa.StorageID
            WHERE pa.ProductID = @productId AND pa.Deleted = 0 AND s.OrganizationID = @organizationId;
            """, new { productId, organizationId });
    }

    private double ReservedForOrder(long orderId) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<double>("""
            SELECT CAST(ISNULL(SUM(r.Qty), 0) AS float)
            FROM dbo.ProductReservation r
            JOIN dbo.OrderItem i ON i.ID = r.OrderItemID
            WHERE i.OrderID = @orderId AND r.Deleted = 0;
            """, new { orderId });
    }

    private long SaleOrderId(long saleId) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<long>("SELECT OrderID FROM dbo.Sale WHERE ID = @saleId;", new { saleId });
    }

    private int OrdersCreatedSince(CheckoutScenario scenario, Watermark watermark) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM dbo.[Order] o JOIN dbo.ClientAgreement ca ON ca.ID = o.ClientAgreementID WHERE ca.ClientID = @ClientId AND o.ID > @OrderId;",
            new { scenario.ClientId, watermark.OrderId });
    }

    private int SalesCreatedSince(CheckoutScenario scenario, Watermark watermark) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM dbo.Sale s JOIN dbo.ClientAgreement ca ON ca.ID = s.ClientAgreementID WHERE ca.ClientID = @ClientId AND s.ID > @SaleId;",
            new { scenario.ClientId, watermark.SaleId });
    }

    private bool OfferProcessed(Guid offerNetUid) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<bool>("SELECT IsOfferProcessed FROM dbo.ClientShoppingCart WHERE NetUID = @offerNetUid;", new { offerNetUid });
    }

    private bool ReceiptExists(Guid operationNetUid) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM dbo.EcommerceSalesCreationLedger WHERE OperationNetUid = @operationNetUid;",
            new { operationNetUid }) == 1;
    }

    private bool ReceiptCompleted(Guid operationNetUid) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM dbo.EcommerceSalesCreationLedger WHERE OperationNetUid = @operationNetUid AND CompletedUtc IS NOT NULL;",
            new { operationNetUid }) == 1;
    }

    private sealed record CheckoutScenario {
        public long ClientId { get; init; }
        public Guid ClientNetUid { get; init; }
        public long AgreementId { get; init; }
        public long ClientAgreementId { get; init; }
        public long OrganizationId { get; init; }
        public bool WithVat { get; init; }
        public List<Guid> ProductNetUids { get; init; } = new();
    }

    private sealed class Watermark {
        public long OrderId { get; init; }
        public long SaleId { get; init; }
    }

    private sealed record TwoCartScenario(
        CheckoutScenario NonVat,
        Guid NonVatProductNetUid,
        CheckoutScenario Vat,
        Guid VatProductNetUid);

    private sealed class CartItemState {
        public long Id { get; init; }
        public long? ClientShoppingCartId { get; init; }
        public long? OrderId { get; init; }
        public bool Deleted { get; init; }
        public double ReservedQty { get; init; }
    }
}
