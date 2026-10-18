using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Search;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.ClientShoppingCartModels;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Infrastructure.Checkout;
using GBA.Services.Services.Clients;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class CartCheckoutRaceTests {
    private const long ProductId = 501;
    private const long CartId = 5;

    private readonly Mock<IDbConnection> _connection = new();
    private readonly Mock<IClientAgreementRepository> _agreements = new();
    private readonly Mock<IAgreementRepository> _agreementRows = new();
    private readonly Mock<IWorkplaceRepository> _workplaces = new();
    private readonly Mock<IClientRepository> _clients = new();
    private readonly Mock<IClientShoppingCartRepository> _carts = new();
    private readonly Mock<IShoppingCartStockRepository> _stock = new();
    private readonly Mock<IOrderItemRepository> _orderItems = new();
    private readonly Mock<IProductAvailabilityRepository> _availabilities = new();
    private readonly Mock<IGetSingleProductRepository> _products = new();
    private readonly Mock<IExchangeRateRepository> _rates = new();
    private readonly Mock<ISearchReindexSignal> _reindex = new();
    private readonly List<string> _calls = new();
    private readonly Guid _clientNetId = Guid.NewGuid();
    private readonly ClientAgreement _agreement = Agreement();
    private readonly Product _product = new() {
        Id = ProductId,
        NetUid = Guid.NewGuid(),
        IsForWeb = true,
        CurrentPrice = 10.123456789m
    };

    public CartCheckoutRaceTests() {
        _agreements.Setup(repository => repository.GetSelectedByClientNetId(_clientNetId)).Returns(_agreement);
        _products
            .Setup(repository => repository.GetProductByNetId(
                _product.NetUid,
                _agreement.NetUid,
                false,
                _agreement.Agreement.CurrencyId,
                _agreement.Agreement.OrganizationId))
            .Returns(() => new Product {
                Id = _product.Id,
                NetUid = _product.NetUid,
                IsForWeb = true,
                CurrentPrice = _product.CurrentPrice
            });
        _rates
            .Setup(repository => repository.GetEuroExchangeRateByCurrentCultureFiltered(
                _product.NetUid, false, false, _agreement.Agreement.CurrencyId!.Value))
            .Returns(50.1234m);
        _clients.Setup(repository => repository.GetByIdWithRegionCode(_agreement.ClientId)).Returns(new Client { ClearCartAfterDays = 3 });
        _stock.Setup(repository => repository.TryLockCartScope(It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<long?>())).Returns(true);
        _stock.Setup(repository => repository.TryLockCart(CartId)).Returns(true);
        _stock
            .Setup(repository => repository.LockProductStock(It.IsAny<IEnumerable<long>>()))
            .Callback<IEnumerable<long>>(ids => _calls.Add("lock:" + string.Join(",", ids)));
        _orderItems
            .Setup(repository => repository.GetByIdAndClientAgreementNetIdWithIncludes(It.IsAny<long>(), _agreement.NetUid, _agreement.Agreement.CurrencyId!.Value))
            .Returns<long, Guid, long>((id, _, _) => new OrderItem { Id = id, ProductId = ProductId, Qty = 1 });
        _orderItems
            .Setup(repository => repository.GetByIdWithIncludes(It.IsAny<long>(), _agreement.NetUid))
            .Returns<long, Guid>((id, _) => new OrderItem { Id = id, ProductId = ProductId, Qty = 1 });
    }

    [Fact]
    public async Task Add_locks_stock_before_reading_it_and_reserves_with_guarded_relative_writes() {
        WithCart();
        WithStock(Availability(11, 2), Availability(12, 5));
        _orderItems.Setup(repository => repository.Add(It.IsAny<OrderItem>())).Returns(77);
        _stock.Setup(repository => repository.Reserve(77, It.IsAny<long>(), It.IsAny<double>())).Returns(true);

        await NewService().Add(NewLine(4), _clientNetId, false);

        Assert.Equal(new[] { "lock:" + ProductId, "read:" + ProductId }, _calls);
        _stock.Verify(repository => repository.Reserve(77, 11, 2), Times.Once);
        _stock.Verify(repository => repository.Reserve(77, 12, 2), Times.Once);
        _stock.Verify(repository => repository.ExtendCart(CartId, DateTime.Now.Date), Times.Once);
        _availabilities.Verify(repository => repository.Update(It.IsAny<ProductAvailability>()), Times.Never);
        _reindex.Verify(signal => signal.Request(ProductId), Times.Once);
    }

    [Fact]
    public async Task A_reservation_that_loses_the_stock_race_rolls_the_add_back_without_side_effects() {
        WithCart();
        WithStock(Availability(11, 2));
        _orderItems.Setup(repository => repository.Add(It.IsAny<OrderItem>())).Returns(77);
        _stock.Setup(repository => repository.Reserve(77, 11, 1)).Returns(false);

        CheckoutConflictException conflict = await Assert.ThrowsAsync<CheckoutConflictException>(() =>
            NewService().Add(NewLine(1), _clientNetId, false));

        Assert.Equal(CheckoutConflictException.CartStockChangedMessage, conflict.Message);
        _reindex.Verify(signal => signal.Request(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Add_rejects_more_than_the_free_stock_before_any_write() {
        WithCart();
        WithStock(Availability(11, 2));

        await Assert.ThrowsAsync<LocalizedException>(() => NewService().Add(NewLine(3), _clientNetId, false));

        _orderItems.Verify(repository => repository.Add(It.IsAny<OrderItem>()), Times.Never);
        _stock.Verify(repository => repository.Reserve(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<double>()), Times.Never);
        _reindex.Verify(signal => signal.Request(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Adding_a_product_already_in_the_cart_increments_the_line_relative_to_the_observed_quantity() {
        WithCart(CartLine(9, ProductId, 2));
        WithStock(Availability(11, 10));
        _stock.Setup(repository => repository.IncreaseItemQty(9, CartId, 2, 3)).Returns(true);
        _stock.Setup(repository => repository.Reserve(9, 11, 3)).Returns(true);

        await NewService().Add(NewLine(3), _clientNetId, false);

        _stock.Verify(repository => repository.IncreaseItemQty(9, CartId, 2, 3), Times.Once);
        _orderItems.Verify(repository => repository.Add(It.IsAny<OrderItem>()), Times.Never);
        _orderItems.Verify(repository => repository.Update(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task A_line_that_changed_since_it_was_read_is_a_conflict() {
        WithCart(CartLine(9, ProductId, 2));
        WithStock(Availability(11, 10));
        _stock.Setup(repository => repository.IncreaseItemQty(9, CartId, 2, 3)).Returns(false);

        await Assert.ThrowsAsync<CheckoutConflictException>(() => NewService().Add(NewLine(3), _clientNetId, false));

        _stock.Verify(repository => repository.Reserve(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<double>()), Times.Never);
    }

    [Fact]
    public async Task The_first_add_creates_the_cart_under_the_cart_scope_lock() {
        WithStock(Availability(11, 10));
        _carts.Setup(repository => repository.Add(It.IsAny<ClientShoppingCart>())).Returns(CartId);
        _orderItems.Setup(repository => repository.Add(It.IsAny<OrderItem>())).Returns(77);
        _stock.Setup(repository => repository.Reserve(77, 11, 1)).Returns(true);

        await NewService().Add(NewLine(1), _clientNetId, false);

        _stock.Verify(repository => repository.TryLockCartScope(_agreement.Id, false, null), Times.Once);
        _carts.Verify(repository => repository.Add(It.Is<ClientShoppingCart>(cart =>
            cart.ClientAgreementId == _agreement.Id &&
            cart.ValidUntil == DateTime.Now.Date.AddDays(3))), Times.Once);
    }

    [Fact]
    public async Task Cart_lines_are_priced_from_the_unrounded_unit_price_like_the_sale_line() {
        WithCart();
        WithStock(Availability(11, 1000));
        OrderItem added = null!;
        _orderItems
            .Setup(repository => repository.Add(It.IsAny<OrderItem>()))
            .Callback<OrderItem>(item => added = item)
            .Returns(77);
        _stock.Setup(repository => repository.Reserve(77, 11, 470)).Returns(true);

        await NewService().Add(NewLine(470), _clientNetId, false);

        Assert.Equal(10.123456789m, added.PricePerItem);
        Assert.Equal(decimal.Round(10.123456789m * 50.1234m * 470m, 2, MidpointRounding.AwayFromZero), added.TotalAmountLocal);
        Assert.Equal(decimal.Round(10.123456789m * 470m, 2, MidpointRounding.AwayFromZero), added.TotalAmount);
        Assert.NotEqual(decimal.Round(decimal.Round(10.123456789m, 2) * 50.1234m, 2) * 470m, added.TotalAmountLocal);
    }

    [Fact]
    public async Task Adding_many_returns_the_added_lines_and_clamps_each_to_its_free_stock() {
        Product second = new() { Id = 502, NetUid = Guid.NewGuid(), IsForWeb = true, CurrentPrice = 2m };
        _products
            .Setup(repository => repository.GetProductByNetId(second.NetUid, _agreement.NetUid, false, _agreement.Agreement.CurrencyId, _agreement.Agreement.OrganizationId))
            .Returns(() => new Product { Id = second.Id, NetUid = second.NetUid, IsForWeb = true, CurrentPrice = 2m });
        _rates
            .Setup(repository => repository.GetEuroExchangeRateByCurrentCultureFiltered(second.NetUid, false, false, _agreement.Agreement.CurrencyId!.Value))
            .Returns(50m);
        WithCart();
        WithStock(Availability(11, 10));
        _availabilities
            .Setup(repository => repository.GetForEcommercePurchase(second.Id, _agreement.Agreement.Organization.Id))
            .Returns(() => new List<ProductAvailability> { Availability(21, 1) });
        _orderItems.SetupSequence(repository => repository.Add(It.IsAny<OrderItem>())).Returns(77).Returns(78);
        _stock.Setup(repository => repository.Reserve(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<double>())).Returns(true);

        List<OrderItem> lines = await NewService().Add(
            new List<OrderItem> {
                NewLine(2),
                new() { Product = new Product { NetUid = second.NetUid }, Qty = 4 }
            },
            _clientNetId,
            false);

        Assert.Equal(new long[] { 77, 78 }, lines.Select(line => line.Id));
        _stock.Verify(repository => repository.Reserve(77, 11, 2), Times.Once);
        _stock.Verify(repository => repository.Reserve(78, 21, 1), Times.Once);
        _stock.Verify(repository => repository.LockProductStock(It.Is<IEnumerable<long>>(ids => ids.OrderBy(id => id).SequenceEqual(new long[] { ProductId, 502 }))), Times.Once);
        _orderItems.Verify(repository => repository.GetByIdAndClientAgreementNetIdWithIncludes(77, _agreement.NetUid, _agreement.Agreement.CurrencyId!.Value), Times.Once);
    }

    [Fact]
    public async Task Lowering_a_quantity_releases_the_newest_reservations_with_guarded_relative_writes() {
        OrderItem line = CartLine(9, ProductId, 4);
        WithCart(line);
        ShoppingCartReservation newest = new() { Id = 31, ProductAvailabilityId = 12, Qty = 1 };
        ShoppingCartReservation oldest = new() { Id = 30, ProductAvailabilityId = 11, Qty = 3 };
        _stock.Setup(repository => repository.GetReservations(9)).Returns(new List<ShoppingCartReservation> { newest, oldest });
        _stock.Setup(repository => repository.Release(It.IsAny<ShoppingCartReservation>(), It.IsAny<double>())).Returns(true);
        _stock.Setup(repository => repository.SetItemQty(9, CartId, 4, 2)).Returns(true);

        await NewService().Update(new OrderItem { Id = 9, Qty = 2, OverLordQty = 2 }, _clientNetId, false);

        _stock.Verify(repository => repository.Release(newest, 1), Times.Once);
        _stock.Verify(repository => repository.Release(oldest, 1), Times.Once);
        _stock.Verify(repository => repository.SetItemQty(9, CartId, 4, 2), Times.Once);
        _availabilities.Verify(repository => repository.Update(It.IsAny<ProductAvailability>()), Times.Never);
        _stock.Verify(repository => repository.ExtendCart(CartId, DateTime.Now.Date), Times.Once);
    }

    [Fact]
    public async Task Raising_a_quantity_reserves_only_the_free_stock_under_the_stock_lock() {
        WithCart(CartLine(9, ProductId, 2));
        WithStock(Availability(11, 1));
        _stock.Setup(repository => repository.Reserve(9, 11, 1)).Returns(true);
        _stock.Setup(repository => repository.SetItemQty(9, CartId, 2, 3)).Returns(true);

        await NewService().Update(new OrderItem { Id = 9, Qty = 5, OverLordQty = 5 }, _clientNetId, false);

        Assert.Equal(new[] { "lock:" + ProductId, "read:" + ProductId }, _calls);
        _stock.Verify(repository => repository.Reserve(9, 11, 1), Times.Once);
        _stock.Verify(repository => repository.SetItemQty(9, CartId, 2, 3), Times.Once);
    }

    [Fact]
    public async Task Updating_resolves_the_cart_through_the_fallback_agreement_when_none_is_selected() {
        Guid otherClient = Guid.NewGuid();
        _agreements.Setup(repository => repository.GetSelectedByClientNotSelectedNetId(otherClient)).Returns(_agreement);
        WithCart(CartLine(9, ProductId, 2));
        _stock.Setup(repository => repository.GetReservations(9)).Returns(new List<ShoppingCartReservation>());
        _stock.Setup(repository => repository.SetItemQty(9, CartId, 2, 1)).Returns(true);

        await NewService().Update(new OrderItem { Id = 9, Qty = 1, OverLordQty = 1 }, otherClient, false);

        _stock.Verify(repository => repository.SetItemQty(9, CartId, 2, 1), Times.Once);
    }

    [Fact]
    public async Task Deleting_an_item_that_checkout_already_moved_to_an_order_releases_nothing() {
        OrderItem item = CartLine(9, ProductId, 2);
        WithCart(item);
        _orderItems.Setup(repository => repository.RemoveFromShoppingCart(item.NetUid, CartId)).Returns(false);

        await NewService().DeleteItemFromShoppingCartByNetId(item.NetUid, _clientNetId, false);

        _stock.Verify(repository => repository.ReleaseAll(It.IsAny<long>()), Times.Never);
        _reindex.Verify(signal => signal.Request(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Deleting_an_item_releases_its_reservations_under_the_stock_lock() {
        OrderItem item = CartLine(9, ProductId, 2);
        WithCart(item);
        _orderItems.Setup(repository => repository.RemoveFromShoppingCart(item.NetUid, CartId)).Returns(true);
        _stock
            .Setup(repository => repository.ReleaseAll(9))
            .Callback(() => _calls.Add("release:9"))
            .Returns(1);

        await NewService().DeleteItemFromShoppingCartByNetId(item.NetUid, _clientNetId, false);

        Assert.Equal(new[] { "lock:" + ProductId, "release:9" }, _calls);
        _reindex.Verify(signal => signal.Request(ProductId), Times.Once);
    }

    [Fact]
    public async Task Clearing_a_cart_releases_only_the_items_still_in_the_cart() {
        OrderItem ordered = CartLine(9, 109, 2);
        OrderItem stillInCart = CartLine(10, 110, 2);
        WithCart(ordered, stillInCart);
        _orderItems.Setup(repository => repository.RemoveFromShoppingCart(ordered.NetUid, CartId)).Returns(false);
        _orderItems.Setup(repository => repository.RemoveFromShoppingCart(stillInCart.NetUid, CartId)).Returns(true);

        await NewService().DeleteAllItemsFromShoppingCartByClientNetId(_clientNetId, false);

        _stock.Verify(repository => repository.ReleaseAll(ordered.Id), Times.Never);
        _stock.Verify(repository => repository.ReleaseAll(stillInCart.Id), Times.Once);
        _stock.Verify(repository => repository.LockProductStock(It.Is<IEnumerable<long>>(ids => ids.OrderBy(id => id).SequenceEqual(new long[] { 109, 110 }))), Times.Once);
    }

    [Fact]
    public async Task Reading_the_cart_never_persists_a_selected_agreement() {
        Guid otherClient = Guid.NewGuid();
        _agreements.Setup(repository => repository.GetSelectedByClientNotSelectedNetId(otherClient)).Returns(_agreement);
        _orderItems
            .Setup(repository => repository.GetAllFromCurrentShoppingByClientNetId(null, _agreement.NetUid, _agreement.Agreement.CurrencyId, _agreement.Agreement.OrganizationId, false))
            .Returns(new List<OrderItem>());

        await NewService().GetAllItemsFromCurrentShoppingCartByClientNetId(otherClient, false);

        _agreementRows.Verify(repository => repository.Update(It.IsAny<Agreement>()), Times.Never);
    }

    [Fact]
    public async Task Expiry_skips_a_cart_that_was_used_after_it_was_listed() {
        _stock.Setup(repository => repository.GetExpiredCartIds(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(new List<long> { CartId });
        _stock.Setup(repository => repository.IsExpired(CartId, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(false);

        int released = await NewService().ReleaseExpiredCartsAsync();

        Assert.Equal(0, released);
        _stock.Verify(repository => repository.GetCartItems(It.IsAny<long>()), Times.Never);
        _stock.Verify(repository => repository.RemoveIfEmpty(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Expiry_releases_every_item_still_in_the_cart_and_removes_only_an_emptied_cart() {
        OrderItem ordered = CartLine(9, 109, 2);
        OrderItem stillInCart = CartLine(10, 110, 2);
        _stock.Setup(repository => repository.GetExpiredCartIds(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(new List<long> { CartId });
        _stock.Setup(repository => repository.IsExpired(CartId, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(true);
        _stock.Setup(repository => repository.GetCartItems(CartId)).Returns(new List<OrderItem> { ordered, stillInCart });
        _orderItems.Setup(repository => repository.RemoveFromShoppingCart(ordered.NetUid, CartId)).Returns(false);
        _orderItems.Setup(repository => repository.RemoveFromShoppingCart(stillInCart.NetUid, CartId)).Returns(true);
        _stock.Setup(repository => repository.ReleaseAll(stillInCart.Id)).Returns(2);

        int released = await NewService().ReleaseExpiredCartsAsync();

        Assert.Equal(2, released);
        _stock.Verify(repository => repository.ReleaseAll(ordered.Id), Times.Never);
        _stock.Verify(repository => repository.RemoveIfEmpty(CartId), Times.Once);
        _carts.Verify(repository => repository.Remove(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task One_contended_cart_does_not_stop_the_expiry_of_the_others() {
        _stock.Setup(repository => repository.GetExpiredCartIds(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(new List<long> { 6, CartId });
        _stock.Setup(repository => repository.TryLockCart(6)).Returns(true);
        _stock.Setup(repository => repository.IsExpired(6, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(true);
        _stock.Setup(repository => repository.GetCartItems(6)).Throws(new CheckoutConflictException(CheckoutConflictException.CartStockChangedMessage));
        _stock.Setup(repository => repository.IsExpired(CartId, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(true);
        _stock.Setup(repository => repository.GetCartItems(CartId)).Returns(new List<OrderItem>());

        await NewService().ReleaseExpiredCartsAsync();

        _stock.Verify(repository => repository.RemoveIfEmpty(CartId), Times.Once);
    }

    private void WithCart(params OrderItem[] items) {
        ClientShoppingCart cart = new() { Id = CartId, NetUid = Guid.NewGuid() };
        foreach (OrderItem item in items) cart.OrderItems.Add(item);
        _carts.Setup(repository => repository.GetByClientAgreementNetId(_agreement.NetUid, false, null)).Returns(cart);
        _stock.Setup(repository => repository.GetCartItems(CartId)).Returns(() => items.ToList());
    }

    private void WithStock(params ProductAvailability[] rows) {
        _availabilities
            .Setup(repository => repository.GetForEcommercePurchase(ProductId, _agreement.Agreement.Organization.Id))
            .Callback(() => _calls.Add("read:" + ProductId))
            .Returns(() => rows.Select(row => new ProductAvailability { Id = row.Id, Amount = row.Amount }).ToList());
    }

    private OrderItem NewLine(double qty) {
        return new OrderItem { Product = new Product { NetUid = _product.NetUid }, Qty = qty, OverLordQty = qty };
    }

    private ClientShoppingCartService NewService() {
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewSqlConnection()).Returns(_connection.Object);

        Mock<IClientRepositoriesFactory> clientRepositories = new();
        clientRepositories.Setup(factory => factory.NewClientAgreementRepository(_connection.Object)).Returns(_agreements.Object);
        clientRepositories.Setup(factory => factory.NewWorkplaceRepository(_connection.Object)).Returns(_workplaces.Object);
        clientRepositories.Setup(factory => factory.NewClientRepository(_connection.Object)).Returns(_clients.Object);
        clientRepositories.Setup(factory => factory.NewClientShoppingCartRepository(_connection.Object)).Returns(_carts.Object);
        clientRepositories.Setup(factory => factory.NewShoppingCartStockRepository(_connection.Object)).Returns(_stock.Object);

        Mock<ISaleRepositoriesFactory> saleRepositories = new();
        saleRepositories.Setup(factory => factory.NewOrderItemRepository(_connection.Object)).Returns(_orderItems.Object);

        Mock<IProductRepositoriesFactory> productRepositories = new();
        productRepositories.Setup(factory => factory.NewProductAvailabilityRepository(_connection.Object)).Returns(_availabilities.Object);
        productRepositories.Setup(factory => factory.NewGetSingleProductRepository(_connection.Object)).Returns(_products.Object);

        Mock<IExchangeRateRepositoriesFactory> rateRepositories = new();
        rateRepositories.Setup(factory => factory.NewExchangeRateRepository(_connection.Object)).Returns(_rates.Object);

        Mock<IAgreementRepositoriesFactory> agreementRepositories = new();
        agreementRepositories.Setup(factory => factory.NewAgreementRepository(_connection.Object)).Returns(_agreementRows.Object);

        return new ClientShoppingCartService(
            clientRepositories.Object,
            saleRepositories.Object,
            productRepositories.Object,
            Mock.Of<IStorageRepositoryFactory>(),
            Mock.Of<IPricingRepositoriesFactory>(),
            rateRepositories.Object,
            Mock.Of<ICurrencyRepositoriesFactory>(),
            connectionFactory.Object,
            agreementRepositories.Object,
            Mock.Of<IHttpClientFactory>(),
            _reindex.Object);
    }

    private static ClientAgreement Agreement() {
        Organization organization = new() { Id = 22 };
        return new ClientAgreement {
            Id = 3,
            ClientId = 40,
            NetUid = Guid.NewGuid(),
            Agreement = new Agreement {
                WithVATAccounting = false,
                CurrencyId = 11,
                OrganizationId = organization.Id,
                Organization = organization
            }
        };
    }

    private static OrderItem CartLine(long id, long productId, double qty) {
        return new OrderItem {
            Id = id,
            NetUid = Guid.NewGuid(),
            ProductId = productId,
            Qty = qty,
            ClientShoppingCartId = CartId,
            Product = new Product { Id = productId, NetUid = Guid.NewGuid() }
        };
    }

    private static ProductAvailability Availability(long id, double amount) {
        return new ProductAvailability { Id = id, Amount = amount, NetUid = Guid.NewGuid() };
    }
}
