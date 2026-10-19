using System.Data;
using System.Transactions;
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

public sealed class CartQuantityReleaseTests {
    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(1.999d)]
    public async Task QuantityDecreaseAbortsWhenTheFullReservationCannotBeReleased(double reserved) {
        var fixture = new Fixture(reserved);
        await Assert.ThrowsAsync<CheckoutConflictException>(() => fixture.ChangeQuantity());
        Assert.Equal(TransactionStatus.Aborted, fixture.Completion);
        fixture.Stock.Verify(x => x.SetItemQty(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<double>(), It.IsAny<double>()), Times.Never);
        fixture.Signal.Verify(x => x.Request(It.IsAny<long>()), Times.Never);
        Assert.Equal(5d, fixture.Current.Qty);
    }

    [Fact]
    public async Task QuantityDecreaseCommitsWhenReservationsCoverTheRequestedDecrease() {
        var fixture = new Fixture(1d, 4d);
        await fixture.ChangeQuantity();
        Assert.Equal(TransactionStatus.Committed, fixture.Completion);
        fixture.Stock.Verify(x => x.Release(It.Is<ShoppingCartReservation>(r => r.Id == 1), 1d), Times.Once);
        fixture.Stock.Verify(x => x.Release(It.Is<ShoppingCartReservation>(r => r.Id == 2), 1d), Times.Once);
        fixture.Stock.Verify(x => x.SetItemQty(10, 20, 5d, 3d), Times.Once);
        Assert.Equal(3d, fixture.Current.Qty);
    }

    [Fact]
    public async Task ConcurrentReleaseConflictAbortsBeforeChangingTheLine() {
        var fixture = new Fixture(5d);
        fixture.Stock.Setup(x => x.Release(It.IsAny<ShoppingCartReservation>(), It.IsAny<double>())).Returns(false);
        await Assert.ThrowsAsync<CheckoutConflictException>(() => fixture.ChangeQuantity());
        Assert.Equal(TransactionStatus.Aborted, fixture.Completion);
        fixture.Stock.Verify(x => x.SetItemQty(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<double>(), It.IsAny<double>()), Times.Never);
    }

    private sealed class Fixture {
        public Mock<IShoppingCartStockRepository> Stock { get; } = new();
        public Mock<ISearchReindexSignal> Signal { get; } = new();
        public TransactionStatus? Completion { get; private set; }
        public OrderItem Current { get; } = new() { Id = 10, ProductId = 30, Qty = 5, Product = new Product { Id = 30 } };
        private readonly ClientShoppingCartService _service;
        private readonly Guid _clientId = Guid.NewGuid();

        public Fixture(params double[] reserved) {
            var connection = new Mock<IDbConnection>();
            connection.Setup(x => x.Open()).Callback(() => {
                Assert.NotNull(Transaction.Current);
                Transaction.Current.TransactionCompleted += (_, args) => Completion = args.Transaction!.TransactionInformation.Status;
            });
            var connections = new Mock<IDbConnectionFactory>();
            connections.Setup(x => x.NewSqlConnection()).Returns(connection.Object);
            var agreement = new ClientAgreement { Id = 1, NetUid = Guid.NewGuid(), Agreement = new Agreement {
                CurrencyId = 1, OrganizationId = 1, Organization = new Organization { Id = 1 }
            }};
            var agreements = new Mock<IClientAgreementRepository>();
            agreements.Setup(x => x.GetSelectedByClientNetId(_clientId)).Returns(agreement);
            var carts = new Mock<IClientShoppingCartRepository>();
            carts.Setup(x => x.GetByClientAgreementNetId(agreement.NetUid, false, null)).Returns(new ClientShoppingCart { Id = 20 });
            var clients = new Mock<IClientRepositoriesFactory>();
            clients.Setup(x => x.NewClientAgreementRepository(connection.Object)).Returns(agreements.Object);
            clients.Setup(x => x.NewWorkplaceRepository(connection.Object)).Returns(Mock.Of<IWorkplaceRepository>());
            clients.Setup(x => x.NewClientShoppingCartRepository(connection.Object)).Returns(carts.Object);
            clients.Setup(x => x.NewShoppingCartStockRepository(connection.Object)).Returns(Stock.Object);
            Stock.Setup(x => x.TryLockCart(20)).Returns(true);
            Stock.Setup(x => x.GetCartItems(20)).Returns(new List<OrderItem> { Current });
            Stock.Setup(x => x.GetReservations(10)).Returns(reserved.Select((qty, index) => new ShoppingCartReservation {
                Id = index + 1, ProductAvailabilityId = 40, Qty = qty
            }).ToList());
            Stock.Setup(x => x.Release(It.IsAny<ShoppingCartReservation>(), It.IsAny<double>())).Returns(true);
            Stock.Setup(x => x.SetItemQty(10, 20, 5d, 3d)).Returns(true);
            var items = new Mock<IOrderItemRepository>();
            items.Setup(x => x.GetByIdWithIncludes(10, agreement.NetUid)).Returns(Current);
            var sales = new Mock<ISaleRepositoriesFactory>();
            sales.Setup(x => x.NewOrderItemRepository(connection.Object)).Returns(items.Object);
            _service = new ClientShoppingCartService(clients.Object, sales.Object, Mock.Of<IProductRepositoriesFactory>(),
                Mock.Of<IStorageRepositoryFactory>(), Mock.Of<IPricingRepositoriesFactory>(), Mock.Of<IExchangeRateRepositoriesFactory>(),
                Mock.Of<ICurrencyRepositoriesFactory>(), connections.Object, Mock.Of<IAgreementRepositoriesFactory>(),
                Mock.Of<IHttpClientFactory>(), Signal.Object);
        }

        public Task<OrderItem> ChangeQuantity() => _service.Update(new OrderItem { Id = 10, Qty = 3, OverLordQty = 3 }, _clientId, false);
    }
}
