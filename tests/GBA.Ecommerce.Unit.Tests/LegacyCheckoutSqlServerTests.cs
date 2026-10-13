using System.Data;
using Dapper;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.Services.Orders;
using Moq;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Services.Services.Offers;
using GBA.Services.Infrastructure.SalesMutations;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Unit.Tests;

[Collection("EcommerceSqlIntegration")]
public sealed class LegacyCheckoutSqlServerTests {
    [Fact]
    public async Task LateFailureRollsBackConsumedItemsAndReceiptAndSameKeyCanRetry() {
        await RunAsync(async fixture => {
            SalesCreationRequest request = fixture.Request(Guid.NewGuid());
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Executor.ExecuteAsync(request, connection => {
                fixture.Create(connection);
                throw new InvalidOperationException("Failure after the cart move and sale write");
            }));
            Assert.Equal(1, await fixture.CartItems());
            Assert.Equal(0, await fixture.Sales());
            Assert.Null(await fixture.Ledger.GetAsync(request.OperationNetUid, default));
            var created = await fixture.Executor.ExecuteAsync(request, fixture.Create);
            Assert.False(created.IsReplay);
            Assert.Equal(0, await fixture.CartItems());
            Assert.Equal(1, await fixture.Sales());
            Assert.NotNull(await fixture.Ledger.GetAsync(request.OperationNetUid, default));
        });
    }

    [Fact]
    public async Task LostResponseReplaysWithoutConsumingRefilledCart() {
        await RunAsync(async fixture => {
            SalesCreationRequest request = fixture.Request(Guid.NewGuid());
            var first = await fixture.Executor.ExecuteAsync(request, fixture.Create);
            await fixture.Refill();
            var replay = await fixture.Executor.ExecuteAsync(request, _ => throw new Exception("Replay must not consume a cart"));
            Assert.True(replay.IsReplay);
            Assert.Equal(first.Sale.NetUid, replay.Sale.NetUid);
            Assert.Equal(1, await fixture.CartItems());
            var next = await fixture.Executor.ExecuteAsync(fixture.Request(Guid.NewGuid()), fixture.Create);
            Assert.False(next.IsReplay);
            Assert.NotEqual(first.Sale.NetUid, next.Sale.NetUid);
            Assert.Equal(2, await fixture.Sales());
        });
    }

    [Fact]
    public async Task ConcurrentIdenticalRequestsCreateOneSaleAndReturnOneReceipt() {
        await RunAsync(async fixture => {
            SalesCreationRequest request = fixture.Request(Guid.NewGuid());
            var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
                Task.Run(() => fixture.Executor.ExecuteAsync(request, fixture.Create))));
            Assert.Single(results, result => !result.IsReplay);
            Assert.Single(results.Select(result => result.Sale.NetUid).Distinct());
            Assert.Equal(1, await fixture.Sales());
        });
    }

    [Fact]
    public async Task DifferentKeysCannotConsumeTheSameCartTwice() {
        await RunAsync(async fixture => {
            async Task<bool> Attempt() {
                try { await fixture.Executor.ExecuteAsync(fixture.Request(Guid.NewGuid()), fixture.Create); return true; }
                catch (ArgumentException) { return false; }
            }
            bool[] results = await Task.WhenAll(Task.Run(Attempt), Task.Run(Attempt));
            Assert.Single(results, result => result);
            Assert.Equal(1, await fixture.Sales());
        });
    }

    [Fact]
    public async Task ReceiptRejectsAnotherPrincipalOrVatMode() {
        await RunAsync(async fixture => {
            Guid key = Guid.NewGuid();
            await fixture.Executor.ExecuteAsync(fixture.Request(key), fixture.Create);
            SalesCreationRequest[] conflicts = {
                LegacyCheckoutExecutor.Request(key, Guid.NewGuid(), "legacy-test-cart", false, new { WithVat = false }),
                LegacyCheckoutExecutor.Request(key, fixture.Principal, "legacy-test-cart", true, new { WithVat = true })
            };
            foreach (SalesCreationRequest conflict in conflicts)
                await Assert.ThrowsAsync<SalesCreationIdempotencyConflictException>(() =>
                    fixture.Executor.ExecuteAsync(conflict, _ => throw new Exception("Conflict reached business writes")));
            Assert.Equal(1, await fixture.Sales());
        });
    }

    [Fact]
    public async Task OwnershipFailureLeavesCartAndReceiptUntouched() {
        await RunAsync(async fixture => {
            SalesCreationRequest request = fixture.Request(Guid.NewGuid());
            await Assert.ThrowsAnyAsync<ArgumentException>(() => fixture.Executor.ExecuteAsync(request, connection => {
                LegacyCheckoutPolicy.RequireCart(connection, fixture.LoadCart(connection), Guid.NewGuid(), false);
                return fixture.Create(connection);
            }));
            Assert.Equal(1, await fixture.CartItems());
            Assert.Equal(0, await fixture.Sales());
            Assert.Null(await fixture.Ledger.GetAsync(request.OperationNetUid, default));
        });
    }

    [Fact]
    public async Task ChangedItemAssignmentRollsBackEarlierItemMoves() {
        await RunAsync(async fixture => {
            SalesCreationRequest request = fixture.Request(Guid.NewGuid());
            await Assert.ThrowsAsync<SalesCreationIdempotencyConflictException>(() => fixture.Executor.ExecuteAsync(request, connection => {
                ClientShoppingCart cart = fixture.LoadCart(connection);
                cart.OrderItems.Add(new OrderItem { Id = fixture.Id + 1, NetUid = Guid.NewGuid(), ProductId = 9, Qty = 1 });
                LegacyCheckoutPolicy.MoveCartItems(connection, cart, fixture.Id);
                throw new Exception("Stale assignment should be refused");
            }));
            Assert.Equal(1, await fixture.CartItems());
            Assert.Null(await fixture.Ledger.GetAsync(request.OperationNetUid, default));
        });
    }

    [Fact]
    public async Task ActualCartServiceRollsBackOnLateReloadFailureThenReplaysSuccess() {
        await RunAsync(async fixture => {
            Guid key = Guid.NewGuid();
            OrderService failing = fixture.CartService(failReload: true);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                failing.GenerateNewOrderAndSaleFromClientShoppingCart(fixture.Principal, false, key));
            Assert.Equal(1, await fixture.CartItems());
            Assert.Equal(0, await fixture.Sales());
            Assert.Null(await fixture.Ledger.GetAsync(key, default));
            OrderService service = fixture.CartService(failReload: false);
            Sale created = await service.GenerateNewOrderAndSaleFromClientShoppingCart(fixture.Principal, false, key);
            Sale replay = await service.GenerateNewOrderAndSaleFromClientShoppingCart(fixture.Principal, false, key);
            Assert.Equal(created.NetUid, replay.NetUid);
            Assert.Equal(fixture.Id, created.ClientAgreementId);
            Assert.Equal(1, await fixture.Sales());
            Assert.Equal(0, await fixture.CartItems());
        });
    }

    [Fact]
    public async Task ActualOfferServiceRollsBackStockReservationsAndProcessedFlagOnLateFailure() {
        await RunAsync(async fixture => {
            await fixture.MakeOffer();
            Guid key = Guid.NewGuid();
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.OfferService(true)
                .GenerateNewOrderAndSaleFromOffer(fixture.OfferRequest(), fixture.Principal, false, key));
            Assert.False(await fixture.OfferProcessed());
            Assert.Equal(10d, await fixture.Stock());
            Assert.Equal(0, await fixture.Reservations());
            Assert.Equal(0, await fixture.Sales());
            Assert.Null(await fixture.Ledger.GetAsync(key, default));
            OfferService service = fixture.OfferService(false);
            Sale first = await service.GenerateNewOrderAndSaleFromOffer(fixture.OfferRequest(), fixture.Principal, false, key);
            Sale replay = await service.GenerateNewOrderAndSaleFromOffer(fixture.OfferRequest(), fixture.Principal, false, key);
            Assert.Equal(first.NetUid, replay.NetUid);
            Assert.True(await fixture.OfferProcessed());
            Assert.Equal(8d, await fixture.Stock());
            Assert.Equal(1, await fixture.Reservations());
            Assert.Equal(1, await fixture.Sales());
            ClientShoppingCart changedPayload = fixture.OfferRequest();
            changedPayload.OrderItems.Single().Qty = -2;
            await Assert.ThrowsAsync<SalesCreationIdempotencyConflictException>(() =>
                service.GenerateNewOrderAndSaleFromOffer(changedPayload, fixture.Principal, false, key));
            Guid rejectedKey = Guid.NewGuid();
            await Assert.ThrowsAsync<LegacyCheckoutRejectedException>(() =>
                service.GenerateNewOrderAndSaleFromOffer(changedPayload, fixture.Principal, false, rejectedKey));
            Assert.Null(await fixture.Ledger.GetAsync(rejectedKey, default));
            Assert.Equal(8d, await fixture.Stock());
            await Assert.ThrowsAnyAsync<ArgumentException>(() => service.GenerateNewOrderAndSaleFromOffer(
                fixture.OfferRequest(), fixture.Principal, false, Guid.NewGuid()));
            Assert.Equal(1, await fixture.Sales());
        });
    }

    [Fact]
    public async Task CartEditorCannotChangeAnItemAlreadyMovedToSale() {
        await RunAsync(async fixture => {
            await fixture.Executor.ExecuteAsync(fixture.Request(Guid.NewGuid()), fixture.Create);
            using IDbConnection connection = fixture.OpenConnection();
            using var mutation = new CartMutationScope(connection);
            Assert.Throws<ArgumentException>(() => CartMutationScope.RequireOwnedItem(connection, fixture.Id, fixture.Principal, 9));
        });
    }

    [Fact]
    public async Task ActualOfferServicePreservesUnreservedBackorderWhenStockRowIsAbsent() {
        await RunAsync(async fixture => {
            await fixture.MakeOffer();
            using (IDbConnection connection = fixture.OpenConnection())
                await connection.ExecuteAsync("DELETE dbo.CheckoutProbeStock WHERE ID=@Id", new { fixture.Id });
            Sale sale = await fixture.OfferService(false).GenerateNewOrderAndSaleFromOffer(
                fixture.OfferRequest(), fixture.Principal, false, Guid.NewGuid());
            Assert.NotEqual(Guid.Empty, sale.NetUid);
            Assert.Equal(0, await fixture.Reservations());
            using IDbConnection verification = fixture.OpenConnection();
            Assert.Equal(2d, await verification.ExecuteScalarAsync<double>(
                "SELECT Qty FROM dbo.OrderItem WHERE ID=@itemId", new { itemId = fixture.Id + 2 }));
        });
    }

    private static async Task RunAsync(Func<Fixture, Task> test) {
        string? connectionString = Environment.GetEnvironmentVariable("GBA_ECOMMERCE_SQL_INTEGRATION_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) {
            if (Environment.GetEnvironmentVariable("GBA_ECOMMERCE_SQL_INTEGRATION_REQUIRED") == "1")
                throw new InvalidOperationException("A disposable SQL integration connection is required.");
            return;
        }
        if (!new SqlConnectionStringBuilder(connectionString).InitialCatalog.Contains("Test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Checkout fixtures require a disposable Test database.");
        await using Fixture fixture = new(connectionString);
        await fixture.Initialize();
        await test(fixture);
    }

    private sealed class Fixture(string connectionString) : IAsyncDisposable {
        private readonly ConnectionFactory factory = new(connectionString);
        public long Id { get; } = Random.Shared.NextInt64(1_000_000, 1_000_000_000);
        public Guid Principal { get; } = Guid.NewGuid();
        private Guid CartUid { get; } = Guid.NewGuid();
        public IDbConnection OpenConnection() => factory.NewSqlConnection();
        public SqlSalesCreationLedgerStore Ledger => new(factory);
        public LegacyCheckoutExecutor Executor => new(factory, Ledger, TimeProvider.System);
        public SalesCreationRequest Request(Guid key) => LegacyCheckoutExecutor.Request(
            key, Principal, "legacy-test-cart", false, new { WithVat = false });
        public async Task Initialize() {
            await Ledger.EnsureSchemaAsync(default);
            using IDbConnection connection = factory.NewSqlConnection();
            await connection.ExecuteAsync("""
                IF OBJECT_ID('dbo.Workplace') IS NULL CREATE TABLE dbo.Workplace(ID bigint PRIMARY KEY,NetUID uniqueidentifier);
                IF OBJECT_ID('dbo.Client') IS NULL CREATE TABLE dbo.Client(ID bigint PRIMARY KEY,NetUID uniqueidentifier,Deleted bit);
                IF OBJECT_ID('dbo.ClientAgreement') IS NULL CREATE TABLE dbo.ClientAgreement(ID bigint PRIMARY KEY,ClientID bigint,Deleted bit);
                IF OBJECT_ID('dbo.ClientShoppingCart') IS NULL CREATE TABLE dbo.ClientShoppingCart(
                    ID bigint PRIMARY KEY,NetUID uniqueidentifier,ClientAgreementID bigint,Deleted bit,IsOffer bit,
                    WorkplaceID bigint NULL,IsOfferProcessed bit,ValidUntil datetime2);
                IF OBJECT_ID('dbo.OrderItem') IS NULL CREATE TABLE dbo.OrderItem(
                    ID bigint PRIMARY KEY,NetUID uniqueidentifier,ClientShoppingCartID bigint NULL,OrderID bigint NULL,
                    ProductID bigint,Qty float,Deleted bit,Updated datetime2);
                IF OBJECT_ID('dbo.CheckoutProbeStock') IS NULL CREATE TABLE dbo.CheckoutProbeStock(ID bigint PRIMARY KEY,Amount float);
                IF OBJECT_ID('dbo.CheckoutProbeReservation') IS NULL CREATE TABLE dbo.CheckoutProbeReservation(StockID bigint,OrderItemID bigint,Qty float);
                IF OBJECT_ID('dbo.SaleNumber') IS NULL CREATE TABLE dbo.SaleNumber(
                    ID bigint IDENTITY PRIMARY KEY,OrganizationID bigint,Value nvarchar(100),Created datetime2);
                IF OBJECT_ID('dbo.CheckoutProbeSale') IS NULL CREATE TABLE dbo.CheckoutProbeSale(
                    ID bigint IDENTITY PRIMARY KEY,NetUID uniqueidentifier,CartID bigint);
                INSERT dbo.CheckoutProbeStock VALUES(@Id,10);
                INSERT dbo.Client VALUES(@Id,@Principal,0);
                INSERT dbo.ClientAgreement VALUES(@Id,@Id,0);
                INSERT dbo.ClientShoppingCart VALUES(@Id,@CartUid,@Id,0,0,NULL,0,DATEADD(day,1,SYSUTCDATETIME()));
                INSERT dbo.OrderItem VALUES(@Id,NEWID(),@Id,NULL,9,2,0,SYSUTCDATETIME());
                """, new { Id, Principal, CartUid });
        }
        public ClientShoppingCart LoadCart(IDbConnection connection) {
            ClientShoppingCart cart = connection.QuerySingle<ClientShoppingCart>(
                "SELECT * FROM dbo.ClientShoppingCart WHERE ID=@Id", new { Id });
            cart.OrderItems = connection.Query<OrderItem>(
                "SELECT * FROM dbo.OrderItem WHERE ClientShoppingCartID=@Id AND Deleted=0", new { Id }).ToList();
            return cart;
        }
        public Sale Create(IDbConnection connection) {
            ClientShoppingCart cart = LoadCart(connection);
            LegacyCheckoutPolicy.RequireCart(connection, cart, Principal, false);
            if (cart.OrderItems.Count == 0) throw new ArgumentException("Cart is empty");
            Guid saleUid = Guid.NewGuid();
            long saleId = connection.QuerySingle<long>(
                "INSERT dbo.CheckoutProbeSale(NetUID,CartID) OUTPUT INSERTED.ID VALUES(@saleUid,@Id)", new { saleUid, Id });
            LegacyCheckoutPolicy.MoveCartItems(connection, cart, saleId);
            return new Sale { Id = saleId, NetUid = saleUid, ClientAgreementId = Id };
        }
        public async Task<int> CartItems() {
            using IDbConnection connection = factory.NewSqlConnection();
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.OrderItem WHERE ClientShoppingCartID=@Id", new { Id });
        }
        public async Task<int> Sales() {
            using IDbConnection connection = factory.NewSqlConnection();
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.CheckoutProbeSale WHERE CartID=@Id", new { Id });
        }
        public async Task Refill() {
            using IDbConnection connection = factory.NewSqlConnection();
            await connection.ExecuteAsync("INSERT dbo.OrderItem VALUES(@nextId,NEWID(),@Id,NULL,9,2,0,SYSUTCDATETIME())", new { Id, nextId = Id + 1 });
        }
        public OrderService CartService(bool failReload) => BuildServices(failReload).Cart;
        public OfferService OfferService(bool failReload) => BuildServices(failReload).Offer;
        public ClientShoppingCart OfferRequest() => new() { NetUid = CartUid,
            OrderItems = new List<OrderItem> { new() { ProductId = 9, Qty = 2 } } };
        public async Task MakeOffer() {
            using IDbConnection connection = factory.NewSqlConnection();
            await connection.ExecuteAsync("UPDATE dbo.ClientShoppingCart SET IsOffer=1 WHERE ID=@Id", new { Id });
        }
        public async Task<bool> OfferProcessed() {
            using IDbConnection connection = factory.NewSqlConnection();
            return await connection.ExecuteScalarAsync<bool>("SELECT IsOfferProcessed FROM dbo.ClientShoppingCart WHERE ID=@Id", new { Id });
        }
        public async Task<double> Stock() {
            using IDbConnection connection = factory.NewSqlConnection();
            return await connection.ExecuteScalarAsync<double>("SELECT Amount FROM dbo.CheckoutProbeStock WHERE ID=@Id", new { Id });
        }
        public async Task<int> Reservations() {
            using IDbConnection connection = factory.NewSqlConnection();
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.CheckoutProbeReservation WHERE StockID=@Id", new { Id });
        }
        private (OrderService Cart, OfferService Offer) BuildServices(bool failReload) {
            var clients = new Mock<IClientRepositoriesFactory>(MockBehavior.Strict);
            var sales = new Mock<ISaleRepositoriesFactory>(MockBehavior.Strict);
            var users = new Mock<IUserRepositoriesFactory>(MockBehavior.Strict);
            var agreement = new ClientAgreement { Id = Id, NetUid = Guid.NewGuid(),
                Agreement = new Agreement { Organization = new Organization { Id = Id, Code = "T" + Id } } };
            clients.Setup(value => value.NewClientShoppingCartRepository(It.IsAny<IDbConnection>()))
                .Returns((IDbConnection connection) => {
                    var repository = new Mock<IClientShoppingCartRepository>(MockBehavior.Strict);
                    repository.Setup(value => value.GetByClientNetId(Principal, false)).Returns(() => LoadCart(connection));
                    repository.Setup(value => value.GetByNetId(CartUid)).Returns(() => LoadCart(connection));
                    repository.Setup(value => value.UpdateProcessingStatus(It.IsAny<ClientShoppingCart>()));
                    repository.Setup(value => value.SetProcessedByNetId(CartUid)).Callback(() =>
                        connection.Execute("UPDATE dbo.ClientShoppingCart SET IsOfferProcessed=1 WHERE ID=@Id", new { Id }));
                    return repository.Object;
                });
            var agreements = new Mock<IClientAgreementRepository>(MockBehavior.Strict);
            agreements.Setup(value => value.GetWithOrganizationById(Id)).Returns(agreement);
            clients.Setup(value => value.NewClientAgreementRepository(It.IsAny<IDbConnection>())).Returns(agreements.Object);
            var user = new Mock<IUserRepository>(MockBehavior.Strict);
            user.Setup(value => value.GetManagerOrGBAIdByClientNetId(Principal)).Returns(1);
            users.Setup(value => value.NewUserRepository(It.IsAny<IDbConnection>())).Returns(user.Object);
            var orders = new Mock<IOrderRepository>(MockBehavior.Strict);
            orders.Setup(value => value.Add(It.IsAny<Order>())).Returns((Order order) => {
                Assert.Equal(Id, order.ClientAgreementId);
                return Id;
            });
            sales.Setup(value => value.NewOrderRepository(It.IsAny<IDbConnection>())).Returns(orders.Object);
            var life = new Mock<IBaseLifeCycleStatusRepository>(MockBehavior.Strict);
            life.Setup(value => value.Add(It.IsAny<GBA.Domain.Entities.Sales.LifeCycleStatuses.BaseLifeCycleStatus>())).Returns(1);
            sales.Setup(value => value.NewBaseLifeCycleStatusRepository(It.IsAny<IDbConnection>())).Returns(life.Object);
            var payment = new Mock<IBaseSalePaymentStatusRepository>(MockBehavior.Strict);
            payment.Setup(value => value.Add(It.IsAny<GBA.Domain.Entities.Sales.PaymentStatuses.BaseSalePaymentStatus>())).Returns(1);
            sales.Setup(value => value.NewBaseSalePaymentStatusRepository(It.IsAny<IDbConnection>())).Returns(payment.Object);
            sales.Setup(value => value.NewSaleNumberRepository(It.IsAny<IDbConnection>())).Returns((IDbConnection connection) => {
                var numbers = new Mock<ISaleNumberRepository>(MockBehavior.Strict);
                numbers.Setup(value => value.Add(It.IsAny<SaleNumber>())).Returns((SaleNumber number) =>
                    connection.QuerySingle<long>("INSERT dbo.SaleNumber(OrganizationID,Value,Created) OUTPUT INSERTED.ID VALUES(@OrganizationId,@Value,SYSUTCDATETIME())", number));
                return numbers.Object;
            });
            sales.Setup(value => value.NewSaleRepository(It.IsAny<IDbConnection>())).Returns((IDbConnection connection) => {
                var repository = new Mock<ISaleRepository>(MockBehavior.Strict);
                repository.Setup(value => value.Add(It.IsAny<Sale>())).Returns((Sale sale) => {
                    Assert.Equal(Id, sale.ClientAgreementId);
                    return connection.QuerySingle<long>("INSERT dbo.CheckoutProbeSale(NetUID,CartID) OUTPUT INSERTED.ID VALUES(NEWID(),@Id)", new { Id });
                });
                repository.Setup(value => value.GetById(It.IsAny<long>(), false)).Returns((long saleId, bool withDeleted) => {
                    if (failReload) throw new InvalidOperationException("Injected sale reload failure");
                    return connection.QuerySingle<Sale>("SELECT ID,NetUID,CartID AS ClientAgreementID FROM dbo.CheckoutProbeSale WHERE ID=@saleId", new { saleId });
                });
                return repository.Object;
            });
            var products = new Mock<IProductRepositoriesFactory>(MockBehavior.Strict);
            products.Setup(value => value.NewProductAvailabilityRepository(It.IsAny<IDbConnection>())).Returns((IDbConnection connection) => {
                var availability = new Mock<IProductAvailabilityRepository>(MockBehavior.Strict);
                availability.Setup(value => value.GetByProductAndOrganizationIds(9, Id, false, false, null))
                    .Returns(() => connection.Query<ProductAvailability>("SELECT ID,Amount FROM dbo.CheckoutProbeStock WHERE ID=@Id", new { Id }));
                availability.Setup(value => value.Update(It.IsAny<ProductAvailability>())).Callback((ProductAvailability row) =>
                    connection.Execute("UPDATE dbo.CheckoutProbeStock SET Amount=@Amount WHERE ID=@Id", row));
                return availability.Object;
            });
            products.Setup(value => value.NewProductReservationRepository(It.IsAny<IDbConnection>())).Returns((IDbConnection connection) => {
                var reservations = new Mock<IProductReservationRepository>(MockBehavior.Strict);
                reservations.Setup(value => value.Add(It.IsAny<ProductReservation>())).Callback((ProductReservation reservation) =>
                    connection.Execute("INSERT dbo.CheckoutProbeReservation VALUES(@ProductAvailabilityId,@OrderItemId,@Qty)", reservation));
                return reservations.Object;
            });
            sales.Setup(value => value.NewOrderItemRepository(It.IsAny<IDbConnection>())).Returns((IDbConnection connection) => {
                var items = new Mock<IOrderItemRepository>(MockBehavior.Strict);
                items.Setup(value => value.Add(It.IsAny<OrderItem>())).Returns((OrderItem item) => {
                    long itemId = Id + 2;
                    connection.Execute("INSERT dbo.OrderItem VALUES(@itemId,NEWID(),NULL,@OrderId,@ProductId,@Qty,0,SYSUTCDATETIME())",
                        new { itemId, item.OrderId, item.ProductId, item.Qty });
                    return itemId;
                });
                items.Setup(value => value.SetOfferProcessingStatuses(It.IsAny<IEnumerable<OrderItem>>()));
                return items.Object;
            });
            var http = new Mock<IHttpClientFactory>();
            http.Setup(value => value.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(new NoNetworkHandler()));
            var cartService = new OrderService(sales.Object, clients.Object, users.Object, products.Object, null!, null!, null!, null!,
                null!, factory, null!, http.Object, null!, Ledger, Mock.Of<ISalesMutationOutboxPublisher>(),
                new SalesMutationOutboxOptions { AllowedInternalBaseUri = "https://crm.example" }, TimeProvider.System);
            return (cartService, new OfferService(clients.Object, sales.Object, products.Object, users.Object,
                factory, http.Object, Ledger, TimeProvider.System));
        }
        public async ValueTask DisposeAsync() {
            using IDbConnection connection = factory.NewSqlConnection();
            await connection.ExecuteAsync("""
                DELETE dbo.OrderItem WHERE ID IN (@Id,@nextId,@offerItemId);
                DELETE dbo.CheckoutProbeReservation WHERE StockID=@Id;
                DELETE dbo.CheckoutProbeStock WHERE ID=@Id;
                DELETE dbo.CheckoutProbeSale WHERE CartID=@Id;
                DELETE dbo.SaleNumber WHERE OrganizationID=@Id;
                DELETE dbo.ClientShoppingCart WHERE ID=@Id;
                DELETE dbo.ClientAgreement WHERE ID=@Id;
                DELETE dbo.Client WHERE ID=@Id;
                DELETE dbo.EcommerceSalesCreationLedger WHERE PrincipalNetUid=@Principal;
                """, new { Id, nextId = Id + 1, offerItemId = Id + 2, Principal });
        }
    }
    private sealed class NoNetworkHandler : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
    private sealed class ConnectionFactory(string connectionString) : IDbConnectionFactory {
        public IDbConnection NewSqlConnection() => new SqlConnection(connectionString);
        public IDbConnection NewDataAnalyticSqlConnection() => throw new NotSupportedException();
        public IDbConnection NewIdentitySqlConnection() => throw new NotSupportedException();
        public IDbConnection NewFenixOneCSqlConnection() => throw new NotSupportedException();
        public IDbConnection NewAmgOneCSqlConnection() => throw new NotSupportedException();
    }
}
