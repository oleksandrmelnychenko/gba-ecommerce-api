using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using GBA.Common.Search;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Clients;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class CartCheckoutRaceTests {
    private readonly Mock<IDbConnection> _connection = new();
    private readonly Mock<IClientAgreementRepository> _agreements = new();
    private readonly Mock<IClientShoppingCartRepository> _carts = new();
    private readonly Mock<IOrderItemRepository> _orderItems = new();
    private readonly Mock<IProductReservationRepository> _reservations = new();
    private readonly Mock<IProductAvailabilityRepository> _availabilities = new();
    private readonly Mock<ISearchReindexSignal> _reindex = new();

    [Fact]
    public async Task Deleting_an_item_that_checkout_already_moved_to_an_order_releases_nothing() {
        Guid clientNetId = Guid.NewGuid();
        OrderItem item = CartItem(9);
        ClientShoppingCart cart = Cart(5, item);
        ClientAgreement agreement = Agreement();
        _agreements.Setup(repository => repository.GetSelectedByClientNetId(clientNetId)).Returns(agreement);
        _carts.Setup(repository => repository.GetByClientAgreementNetId(agreement.NetUid, false, null)).Returns(cart);
        _orderItems.Setup(repository => repository.GetByNetIdWithProduct(item.NetUid)).Returns(item);
        _orderItems.Setup(repository => repository.RemoveFromShoppingCart(item.NetUid, cart.Id)).Returns(false);

        await NewService().DeleteItemFromShoppingCartByNetId(item.NetUid, clientNetId, false);

        _reservations.Verify(repository => repository.GetAllByOrderItemIdWithAvailability(It.IsAny<long>()), Times.Never);
        _availabilities.Verify(repository => repository.Update(It.IsAny<ProductAvailability>()), Times.Never);
        _reindex.Verify(signal => signal.Request(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Clearing_a_cart_releases_only_the_items_still_in_the_cart() {
        Guid clientNetId = Guid.NewGuid();
        OrderItem ordered = CartItem(9);
        OrderItem stillInCart = CartItem(10);
        ClientShoppingCart cart = Cart(5, ordered, stillInCart);
        ClientAgreement agreement = Agreement();
        ProductReservation reservation = Reservation(2, 1);
        _agreements.Setup(repository => repository.GetSelectedByClientNetId(clientNetId)).Returns(agreement);
        _carts.Setup(repository => repository.GetByClientAgreementNetId(agreement.NetUid, false, null)).Returns(cart);
        _orderItems.Setup(repository => repository.RemoveFromShoppingCart(ordered.NetUid, cart.Id)).Returns(false);
        _orderItems.Setup(repository => repository.RemoveFromShoppingCart(stillInCart.NetUid, cart.Id)).Returns(true);
        _reservations
            .Setup(repository => repository.GetAllByOrderItemIdWithAvailability(stillInCart.Id))
            .Returns(new[] { reservation });

        await NewService().DeleteAllItemsFromShoppingCartByClientNetId(clientNetId, false);

        _reservations.Verify(repository => repository.GetAllByOrderItemIdWithAvailability(ordered.Id), Times.Never);
        _reservations.Verify(repository => repository.Delete(reservation.NetUid), Times.Once);
        Assert.Equal(3, reservation.ProductAvailability.Amount);
    }

    [Fact]
    public async Task Expiring_a_cart_never_releases_stock_of_an_item_that_was_ordered_concurrently() {
        OrderItem ordered = CartItem(9);
        OrderItem stillInCart = CartItem(10);
        ClientShoppingCart cart = Cart(5, ordered, stillInCart);
        ProductReservation reservation = Reservation(4, 0);
        _carts.Setup(repository => repository.GetAllExistingExpiredClientShoppingCarts())
            .Returns(new List<ClientShoppingCart> { cart });
        _orderItems.Setup(repository => repository.RemoveFromShoppingCart(ordered.NetUid, cart.Id)).Returns(false);
        _orderItems.Setup(repository => repository.RemoveFromShoppingCart(stillInCart.NetUid, cart.Id)).Returns(true);
        _reservations
            .Setup(repository => repository.GetAllByOrderItemIdWithAvailability(stillInCart.Id))
            .Returns(new[] { reservation });

        int released = await NewService().ReleaseExpiredCartsAsync();

        Assert.Equal(1, released);
        _reservations.Verify(repository => repository.GetAllByOrderItemIdWithAvailability(ordered.Id), Times.Never);
        _orderItems.Verify(repository => repository.Remove(It.IsAny<Guid>()), Times.Never);
        Assert.Equal(4, reservation.ProductAvailability.Amount);
        _carts.Verify(repository => repository.Remove(cart.NetUid), Times.Once);
    }

    private ClientShoppingCartService NewService() {
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewSqlConnection()).Returns(_connection.Object);

        Mock<IClientRepositoriesFactory> clientRepositories = new();
        clientRepositories.Setup(factory => factory.NewClientAgreementRepository(_connection.Object)).Returns(_agreements.Object);
        clientRepositories.Setup(factory => factory.NewClientShoppingCartRepository(_connection.Object)).Returns(_carts.Object);

        Mock<ISaleRepositoriesFactory> saleRepositories = new();
        saleRepositories.Setup(factory => factory.NewOrderItemRepository(_connection.Object)).Returns(_orderItems.Object);

        Mock<IProductRepositoriesFactory> productRepositories = new();
        productRepositories.Setup(factory => factory.NewProductReservationRepository(_connection.Object)).Returns(_reservations.Object);
        productRepositories.Setup(factory => factory.NewProductAvailabilityRepository(_connection.Object)).Returns(_availabilities.Object);

        return new ClientShoppingCartService(
            clientRepositories.Object,
            saleRepositories.Object,
            productRepositories.Object,
            Mock.Of<IStorageRepositoryFactory>(),
            Mock.Of<IPricingRepositoriesFactory>(),
            Mock.Of<IExchangeRateRepositoriesFactory>(),
            Mock.Of<ICurrencyRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IAgreementRepositoriesFactory>(),
            Mock.Of<IHttpClientFactory>(),
            _reindex.Object);
    }

    private static ClientAgreement Agreement() {
        return new ClientAgreement {
            Id = 3,
            NetUid = Guid.NewGuid(),
            Agreement = new Agreement { WithVATAccounting = false }
        };
    }

    private static ClientShoppingCart Cart(long id, params OrderItem[] items) {
        ClientShoppingCart cart = new() { Id = id, NetUid = Guid.NewGuid() };
        foreach (OrderItem item in items) cart.OrderItems.Add(item);
        return cart;
    }

    private static OrderItem CartItem(long id) {
        return new OrderItem {
            Id = id,
            NetUid = Guid.NewGuid(),
            ProductId = 100 + id,
            Qty = 2,
            Product = new Product { Id = 100 + id, NetUid = Guid.NewGuid() }
        };
    }

    private static ProductReservation Reservation(double qty, double available) {
        return new ProductReservation {
            NetUid = Guid.NewGuid(),
            Qty = qty,
            ProductAvailability = new ProductAvailability { NetUid = Guid.NewGuid(), Amount = available }
        };
    }
}
