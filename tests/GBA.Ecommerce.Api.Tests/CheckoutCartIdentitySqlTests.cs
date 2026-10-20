using System.Data;
using System.Transactions;
using Dapper;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Common.Search;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Clients;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.Delivery.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Sales;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.Infrastructure.Checkout;
using GBA.Services.Services.Clients;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.Messengers.Contracts;
using GBA.Services.Services.Orders;
using GBA.Services.Services.Offers;
using GBA.Domain.EntityHelpers;
using Moq;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Api.Tests;

[Collection(CheckoutSqlCollection.Name)]
public sealed class CheckoutCartIdentitySqlTests : IDisposable {
    private readonly CheckoutSqlFixture fixture;
    private readonly string? databaseName;
    private readonly string? connectionString;

    public CheckoutCartIdentitySqlTests(CheckoutSqlFixture fixture) {
        this.fixture = fixture;
        if (!fixture.IsConfigured) return;

        string name = "GbaCheckoutCartTest_" + Guid.NewGuid().ToString("N");
        using var server = fixture.Open();
        server.Execute($"CREATE DATABASE [{name}]");
        databaseName = name;
        connectionString = new SqlConnectionStringBuilder(fixture.ConnectionString) {
            InitialCatalog = name, Pooling = false
        }.ConnectionString;
        try {
            using var db = Open();
            string script = File.ReadAllText(Path.Combine(CheckoutSqlFixture.FindRepositoryRoot(), "db/ecommerce-sales-creation-ledger.sql"));
            foreach (string batch in CheckoutSqlFixture.SplitBatches(script)) db.Execute(batch);
        } catch {
            Dispose();
            throw;
        }
    }

    private SqlConnection Open() {
        var connection = new SqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    public void Dispose() {
        if (databaseName == null) return;
        using var server = fixture.Open();
        server.Execute($"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]");
    }
    [CheckoutCartSqlFact]
    public Task CheckoutOfTheSecondLineDoesNotRewriteTheFirstLineOfTheSameProduct() => VerifyTransfer(false);

    [CheckoutCartSqlFact]
    public Task CheckoutKeepsBothRowsOfTheSameProductAndReplaysWithoutMovingThemAgain() => VerifyTransfer(true);

    private async Task VerifyTransfer(bool both) {
        Assert.True(fixture.IsConfigured);
        using var db = Open();
        db.Execute(Schema);
        var ids = db.Query<Guid>("SELECT NetUID FROM dbo.OrderItem ORDER BY ID").ToArray();
        Guid actor = Guid.NewGuid();
        var attempt = new CheckoutAttempt(Guid.NewGuid(), CheckoutFingerprint.Compute("identity", both.ToString()));
        OrderService service = CreateService(actor);
        Sale Request() => new() { Order = new Order { OrderItems = (both ? ids : ids.Skip(1)).Select(id =>
            new OrderItem { NetUid = id, Id = 999, Qty = 999, ProductId = 999 }).ToList() } };

        CheckoutResult result = await service.GenerateNewSaleWithInvoice(Request(), actor, false, false, attempt);
        var rows = db.Query<OrderItem>("SELECT * FROM dbo.OrderItem ORDER BY ID").ToArray();
        Assert.Equal(new long[] { 1, 2 }, rows.Select(x => x.Id));
        Assert.Equal(new double[] { 2, 5 }, rows.Select(x => x.Qty));
        Assert.Equal(both ? (long?)100 : null, rows[0].OrderId);
        Assert.Equal(both ? null : (long?)10, rows[0].ClientShoppingCartId);
        Assert.Equal(100, rows[1].OrderId);
        Assert.Null(rows[1].ClientShoppingCartId);
        Assert.Equal(both ? 12m : 9m, rows[0].PricePerItem);
        Assert.Equal(12m, rows[1].PricePerItem);
        Assert.Equal(new double[] { 2, 5 }, db.Query<double>("SELECT Qty FROM dbo.ProductReservation ORDER BY OrderItemID"));
        Assert.Equal(10d, db.ExecuteScalar<double>("SELECT Amount FROM dbo.ProductAvailability"));
        CheckoutResult replay = await service.GenerateNewSaleWithInvoice(Request(), actor, false, false, attempt);
        Assert.True(replay.IsReplay);
        Assert.Equal(result.Sale.NetUid, replay.Sale.NetUid);
        Assert.Equal(1, db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.EcommerceSalesCreationLedger WHERE OperationNetUid=@Id", new { Id = attempt.IdempotencyKey }));
    }

    [CheckoutCartSqlFact]
    public void CartReadersExcludeRowsAlreadyOwnedByAnOrder() {
        Assert.True(fixture.IsConfigured);
        using var db = Open();
        db.Execute(Schema);
        db.Execute("UPDATE dbo.OrderItem SET OrderID=100 WHERE ID=1");
        Guid clientId = db.ExecuteScalar<Guid>("SELECT NetUID FROM dbo.Client");
        Guid agreementId = db.ExecuteScalar<Guid>("SELECT NetUID FROM dbo.ClientAgreement");
        var repository = new ClientShoppingCartRepository(db);
        Assert.Equal(2, Assert.Single(repository.GetByClientNetId(clientId, false).OrderItems).Id);
        Assert.Equal(2, Assert.Single(repository.GetByClientAgreementNetId(agreementId, false).OrderItems).Id);
    }

    [CheckoutCartSqlFact]
    public void ClaimRejectsMismatchedInternalIdentityWithoutMovingEitherRow() {
        Assert.True(fixture.IsConfigured);
        using var setup = Open();
        setup.Execute(Schema);
        Guid second = setup.ExecuteScalar<Guid>("SELECT NetUID FROM dbo.OrderItem WHERE ID=2");
        using (var scope = CheckoutTransaction.Begin()) {
            using var db = Open();
            Assert.Throws<CheckoutConflictException>(() => CheckoutLocks.ClaimCartItem(db, 10,
                new OrderItem { Id = 1, NetUid = second, Qty = 5 }, 5, 100));
        }
        Assert.Equal(2, setup.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.OrderItem WHERE ClientShoppingCartID=10 AND OrderID IS NULL"));
    }

    [CheckoutCartSqlFact]
    public void AStaleSecondLineRollsBackTheEarlierClaim() {
        Assert.True(fixture.IsConfigured);
        using var setup = Open();
        setup.Execute(Schema);
        var rows = setup.Query<OrderItem>("SELECT * FROM dbo.OrderItem ORDER BY ID").ToArray();
        using (var scope = CheckoutTransaction.Begin()) {
            using var db = Open();
            CheckoutLocks.ClaimCartItem(db, 10, rows[0], 2, 100);
            Assert.Throws<CheckoutConflictException>(() => CheckoutLocks.ClaimCartItem(db, 10, rows[1], 4, 100));
        }
        Assert.Equal(2, setup.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.OrderItem WHERE ClientShoppingCartID=10 AND OrderID IS NULL"));
    }

    [CheckoutCartSqlFact]
    public async Task FailedSaleReloadRollsBackCartTransferAndTheSameKeyCanBeRetried() {
        Assert.True(fixture.IsConfigured);
        using var db = Open();
        db.Execute(Schema);
        Guid item = db.ExecuteScalar<Guid>("SELECT NetUID FROM dbo.OrderItem WHERE ID=2");
        Guid actor = Guid.NewGuid();
        var attempt = new CheckoutAttempt(Guid.NewGuid(), CheckoutFingerprint.Compute("rollback"));
        Sale Request() => new() { Order = new Order { OrderItems = new List<OrderItem> { new() { NetUid = item } } } };
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(actor, failReload: true)
            .GenerateNewSaleWithInvoice(Request(), actor, false, false, attempt));
        Assert.Equal(2, db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.OrderItem WHERE ClientShoppingCartID=10 AND OrderID IS NULL AND PricePerItem=9"));
        Assert.Equal(0, db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.EcommerceSalesCreationLedger WHERE OperationNetUid=@Id", new { Id = attempt.IdempotencyKey }));
        CheckoutResult retried = await CreateService(actor).GenerateNewSaleWithInvoice(Request(), actor, false, false, attempt);
        Assert.False(retried.IsReplay);
        Assert.Equal(100, db.ExecuteScalar<long>("SELECT OrderID FROM dbo.OrderItem WHERE ID=2"));
    }

    [CheckoutCartSqlFact]
    public async Task NullRequestedLineIsRejectedBeforeClaimingTheCart() {
        Assert.True(fixture.IsConfigured);
        using var db = Open();
        db.Execute(Schema);
        Guid actor = Guid.NewGuid();
        var attempt = new CheckoutAttempt(Guid.NewGuid(), CheckoutFingerprint.Compute("null-line"));
        var request = new Sale { Order = new Order { OrderItems = new List<OrderItem> { null! } } };
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService(actor)
            .GenerateNewSaleWithInvoice(request, actor, false, false, attempt));
        Assert.Equal(2, db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.OrderItem WHERE ClientShoppingCartID=10 AND OrderID IS NULL"));
        Assert.Equal(0, db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.EcommerceSalesCreationLedger WHERE OperationNetUid=@Id", new { Id = attempt.IdempotencyKey }));
    }

    [CheckoutCartSqlFact]
    public Task OfferCheckoutUpdatesOnlyTheSelectedRowOfTheSameProduct() => VerifyOfferTransfer(false, 10);

    [CheckoutCartSqlFact]
    public Task OfferCheckoutKeepsSourceRowsWhenSelectionOrderIsReversed() => VerifyOfferTransfer(true, 10);

    [CheckoutCartSqlFact]
    public Task OfferCheckoutRecordsAvailableQuantityAgainstTheSelectedSourceRow() => VerifyOfferTransfer(false, 2);

    [CheckoutCartSqlFact]
    public Task OfferCheckoutMarksTheSelectedRowFullyProcessed() => VerifyOfferTransfer(false, 10, requestedQty: 5);

    [CheckoutCartSqlFact]
    public Task FailedOfferCheckoutRollsBackStatusesStockAndReceiptBeforeRetry() => VerifyOfferTransfer(false, 10, true);

    private async Task VerifyOfferTransfer(bool both, double available, bool failFirst = false, double requestedQty = 3) {
        Assert.True(fixture.IsConfigured);
        using var db = Open();
        db.Execute(Schema);
        db.Execute("""
            ALTER TABLE dbo.OrderItem ADD OrderedQty float NULL,FromOfferQty float NULL,IsFromOffer bit NULL,
                OfferProcessingStatus int NOT NULL DEFAULT 0,OfferProcessingStatusChangedById bigint NULL,
                OneTimeDiscount decimal(30,14) NULL,ReturnedQty float NULL,AssignedSpecificationId bigint NULL,
                IsFromReSale bit NULL,MisplacedSaleId bigint NULL,IsFromShiftedItem bit NULL;
            ALTER TABLE dbo.ClientShoppingCart ADD NetUID uniqueidentifier NULL,IsOfferProcessed bit NOT NULL DEFAULT 0,
                Updated datetime NULL,OfferProcessingStatus int NULL,OfferProcessingStatusChangedById bigint NULL,Comment nvarchar(450) NULL;
            """);
        Guid offerId = Guid.NewGuid();
        db.Execute("UPDATE dbo.ClientShoppingCart SET NetUID=@offerId,IsOffer=1; UPDATE dbo.ProductAvailability SET Amount=@available; DELETE dbo.ProductReservation;",
            new { offerId, available });
        Guid[] ids = db.Query<Guid>("SELECT NetUID FROM dbo.OrderItem ORDER BY ID").ToArray();
        Guid actor = Guid.NewGuid();
        var attempt = new CheckoutAttempt(Guid.NewGuid(), CheckoutFingerprint.Compute("offer-identity", both.ToString()));
        ClientShoppingCart Request() {
            var cart = new ClientShoppingCart { NetUid = offerId };
            cart.OrderItems.Add(new OrderItem { NetUid = ids[1], Qty = requestedQty, ProductId = 999, PricePerItem = 999 });
            if (both) cart.OrderItems.Add(new OrderItem { NetUid = ids[0], Qty = 1, ProductId = 999 });
            return cart;
        }

        if (failFirst) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => CreateOfferService(actor, true)
                .GenerateNewOrderAndSaleFromOffer(Request(), actor, false, attempt));
            Assert.False(db.ExecuteScalar<bool>("SELECT IsOfferProcessed FROM dbo.ClientShoppingCart"));
            Assert.Equal(2, db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.OrderItem WHERE OrderID IS NULL AND OfferProcessingStatus=0"));
            Assert.Equal(2, db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.OrderItem"));
            Assert.Equal(available, db.ExecuteScalar<double>("SELECT Amount FROM dbo.ProductAvailability"));
            Assert.Equal(0, db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.ProductReservation"));
            Assert.Equal(0, db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.EcommerceSalesCreationLedger"));
        }

        OfferService service = CreateOfferService(actor);
        CheckoutResult result = await service.GenerateNewOrderAndSaleFromOffer(Request(), actor, false, attempt);
        OrderItem[] sources = db.Query<OrderItem>("SELECT * FROM dbo.OrderItem WHERE ID IN (1,2) ORDER BY ID").ToArray();
        Assert.Equal(new double[] { 2, 5 }, sources.Select(x => x.Qty));
        Assert.Equal(both ? OfferProcessingStatus.PartiallyProcessed : OfferProcessingStatus.NotProcessed, sources[0].OfferProcessingStatus);
        Assert.Equal(both ? 1d : 0d, sources[0].OrderedQty);
        double secondQty = Math.Min(requestedQty, available);
        Assert.Equal(secondQty >= 5 ? OfferProcessingStatus.FullyProcessed : OfferProcessingStatus.PartiallyProcessed, sources[1].OfferProcessingStatus);
        Assert.Equal(secondQty, sources[1].OrderedQty);
        OrderItem[] saleRows = db.Query<OrderItem>("SELECT * FROM dbo.OrderItem WHERE OrderID=100 ORDER BY ID").ToArray();
        Assert.Equal(both ? 2 : 1, saleRows.Length);
        Assert.Equal(5d, saleRows[0].FromOfferQty);
        Assert.Equal(secondQty, saleRows[0].Qty);
        Assert.Equal(requestedQty, saleRows[0].OrderedQty);
        Assert.All(saleRows, row => { Assert.True(row.IsFromOffer); Assert.Equal(9m, row.PricePerItem); });
        if (both) { Assert.Equal(2d, saleRows[1].FromOfferQty); Assert.Equal(1d, saleRows[1].Qty); }
        double consumed = secondQty + (both ? 1 : 0);
        Assert.Equal(available - consumed, db.ExecuteScalar<double>("SELECT Amount FROM dbo.ProductAvailability"));
        Assert.Equal(consumed, db.ExecuteScalar<double>("SELECT SUM(Qty) FROM dbo.ProductReservation"));
        Assert.True(db.ExecuteScalar<bool>("SELECT IsOfferProcessed FROM dbo.ClientShoppingCart"));
        CheckoutResult replay = await service.GenerateNewOrderAndSaleFromOffer(Request(), actor, false, attempt);
        Assert.True(replay.IsReplay);
        Assert.Equal(result.Sale.NetUid, replay.Sale.NetUid);
        Assert.Equal(saleRows.Length, db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.OrderItem WHERE OrderID=100"));
        Assert.Equal(available - consumed, db.ExecuteScalar<double>("SELECT Amount FROM dbo.ProductAvailability"));
    }

    private OfferService CreateOfferService(Guid actor, bool failReload = false) {
        IDbConnection db = null!;
        var agreement = new ClientAgreement { Id = 20, NetUid = Guid.NewGuid(), Agreement = new Agreement {
            Organization = new Organization { Id = 1, NetUid = Guid.NewGuid(), Code = "T" }
        }};
        var clients = new Mock<IClientRepositoriesFactory>();
        var carts = new Mock<IClientShoppingCartRepository>();
        clients.Setup(x => x.NewClientShoppingCartRepository(It.IsAny<IDbConnection>())).Callback<IDbConnection>(c => db = c).Returns(carts.Object);
        carts.Setup(x => x.GetByNetId(It.IsAny<Guid>())).Returns((Guid id) => {
            var offer = db.QuerySingle<ClientShoppingCart>("SELECT * FROM dbo.ClientShoppingCart WHERE NetUID=@id", new { id });
            offer.ClientAgreement = agreement;
            offer.CreatedById = 1;
            offer.ValidUntil = DateTime.Today.AddDays(1);
            offer.OrderItems = db.Query<OrderItem>("SELECT * FROM dbo.OrderItem WHERE ClientShoppingCartID=10 ORDER BY ID").Select(item => {
                item.Product = new Product { Id = 7, NetUid = Guid.NewGuid() }; return item;
            }).ToList();
            return offer;
        });
        carts.Setup(x => x.TrySetProcessedByNetId(It.IsAny<Guid>())).Returns((Guid id) => new ClientShoppingCartRepository(db).TrySetProcessedByNetId(id));
        carts.Setup(x => x.UpdateProcessingStatus(It.IsAny<ClientShoppingCart>())).Callback<ClientShoppingCart>(offer => new ClientShoppingCartRepository(db).UpdateProcessingStatus(offer));
        var products = new Mock<IProductRepositoriesFactory> { DefaultValue = DefaultValue.Mock };
        products.Setup(x => x.NewProductAvailabilityRepository(It.IsAny<IDbConnection>()).GetForEcommercePurchase(7, 1))
            .Returns(() => db.Query<ProductAvailability>("SELECT * FROM dbo.ProductAvailability").ToList());
        products.Setup(x => x.NewProductAvailabilityRepository(It.IsAny<IDbConnection>()).DecreaseAmount(It.IsAny<ProductAvailability>(), It.IsAny<double>()))
            .Callback<ProductAvailability, double>((item, qty) => Assert.Equal(1, db.Execute(
                "UPDATE dbo.ProductAvailability SET Amount=Amount-@Qty WHERE ID=@Id AND Amount>=@Qty", new { item.Id, Qty = qty })));
        products.Setup(x => x.NewProductReservationRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<ProductReservation>()))
            .Callback<ProductReservation>(item => db.Execute("INSERT dbo.ProductReservation(ID,OrderItemID,Qty) VALUES(@OrderItemId,@OrderItemId,@Qty)", item));
        var sales = new Mock<ISaleRepositoriesFactory> { DefaultValue = DefaultValue.Mock };
        sales.Setup(x => x.NewOrderRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<Order>())).Returns(100);
        sales.Setup(x => x.NewOrderItemRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<OrderItem>()))
            .Returns((OrderItem item) => new OrderItemRepository(db).Add(item));
        sales.Setup(x => x.NewOrderItemRepository(It.IsAny<IDbConnection>()).SetOfferProcessingStatuses(It.IsAny<IEnumerable<OrderItem>>()))
            .Callback<IEnumerable<OrderItem>>(items => new OrderItemRepository(db).SetOfferProcessingStatuses(items));
        sales.Setup(x => x.NewBaseLifeCycleStatusRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<BaseLifeCycleStatus>())).Returns(1);
        sales.Setup(x => x.NewBaseSalePaymentStatusRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<BaseSalePaymentStatus>())).Returns(1);
        sales.Setup(x => x.NewSaleNumberRepository(It.IsAny<IDbConnection>()).GetLastGeneratedByOrganizationNetId(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>())).Returns((SaleNumber)null!);
        sales.Setup(x => x.NewSaleNumberRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<SaleNumber>())).Returns(1);
        sales.Setup(x => x.NewSaleRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<Sale>())).Returns(200);
        sales.Setup(x => x.NewSaleRepository(It.IsAny<IDbConnection>()).GetById(200)).Returns(() => failReload
            ? throw new InvalidOperationException("Synthetic offer sale reload failure.") : new Sale { Id = 200, NetUid = Guid.NewGuid() });
        var access = new Mock<IClientResourceAccessService>();
        access.Setup(x => x.CanAccessClientOrAgreement(actor, agreement.NetUid)).Returns(true);
        var connections = new Mock<IDbConnectionFactory>();
        connections.Setup(x => x.NewSqlConnection()).Returns(() => new SqlConnection(connectionString));
        return new OfferService(clients.Object, sales.Object, products.Object,
            Mock.Of<IUserRepositoriesFactory>(MockBehavior.Loose), connections.Object, new RecordingCrmHandler().Factory(), access.Object,
            new CheckoutExecutor(connections.Object, new SqlCheckoutLedger()));
    }

    private OrderService CreateService(Guid actor, bool failReload = false) {
        IDbConnection db = null!;
        Guid productId = Guid.NewGuid();
        var agreement = new ClientAgreement { Id = 20, NetUid = Guid.NewGuid(), Agreement = new Agreement {
            CurrencyId = 1, OrganizationId = 1, Organization = new Organization { Id = 1, NetUid = Guid.NewGuid(), Code = "T" }
        }};
        var clients = new Mock<IClientRepositoriesFactory> { DefaultValue = DefaultValue.Mock };
        clients.Setup(x => x.NewClientAgreementRepository(It.IsAny<IDbConnection>()).GetSelectedByClientNetId(actor)).Returns(agreement);
        clients.Setup(x => x.NewClientRepository(It.IsAny<IDbConnection>()).GetByNetIdWithRegionCode(actor)).Returns(new Client());
        var carts = new Mock<IClientShoppingCartRepository>();
        clients.Setup(x => x.NewClientShoppingCartRepository(It.IsAny<IDbConnection>())).Callback<IDbConnection>(c => db = c).Returns(carts.Object);
        carts.Setup(x => x.GetByClientAgreementNetId(agreement.NetUid, false, null)).Returns(() => new ClientShoppingCart {
            Id = 10, ClientAgreementId = 20, OrderItems = db.Query<OrderItem>(
                "SELECT * FROM dbo.OrderItem WHERE ClientShoppingCartID=10 AND OrderID IS NULL ORDER BY ID").Select(x => {
                    x.Product = new Product { Id = 7, NetUid = productId }; return x;
                }).ToList()
        });
        var products = new Mock<IProductRepositoriesFactory> { DefaultValue = DefaultValue.Mock };
        products.Setup(x => x.NewGetSingleProductRepository(It.IsAny<IDbConnection>()).GetProductByNetId(productId, agreement.NetUid, false, 1, 1))
            .Returns(() => new Product { Id = 7, NetUid = productId, IsForWeb = true, CurrentPrice = 12m });
        var rates = new Mock<IExchangeRateRepositoriesFactory> { DefaultValue = DefaultValue.Mock };
        rates.Setup(x => x.NewExchangeRateRepository(It.IsAny<IDbConnection>()).GetEuroExchangeRateByCurrentCultureFiltered(productId, false, false, 1)).Returns(50m);
        var sales = new Mock<ISaleRepositoriesFactory> { DefaultValue = DefaultValue.Mock };
        sales.Setup(x => x.NewOrderRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<Order>())).Returns(100);
        sales.Setup(x => x.NewOrderItemRepository(It.IsAny<IDbConnection>()).Update(It.IsAny<OrderItem>())).Callback<OrderItem>(item =>
            new OrderItemRepository(db).Update(item));
        sales.Setup(x => x.NewBaseLifeCycleStatusRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<BaseLifeCycleStatus>())).Returns(1);
        sales.Setup(x => x.NewBaseSalePaymentStatusRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<BaseSalePaymentStatus>())).Returns(1);
        sales.Setup(x => x.NewSaleNumberRepository(It.IsAny<IDbConnection>()).GetLastGeneratedByOrganizationNetId(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>())).Returns((SaleNumber)null!);
        sales.Setup(x => x.NewSaleNumberRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<SaleNumber>())).Returns(1);
        sales.Setup(x => x.NewSaleRepository(It.IsAny<IDbConnection>()).Add(It.IsAny<Sale>())).Returns(200);
        sales.Setup(x => x.NewSaleRepository(It.IsAny<IDbConnection>()).GetByIdWithCalculatedDynamicPrices(200)).Returns(() => failReload
            ? throw new InvalidOperationException("Synthetic sale reload failure.") : new Sale {
            Id = 200, NetUid = Guid.NewGuid(), OrderId = 100, Order = new Order { Id = 100,
                OrderItems = db.Query<OrderItem>("SELECT * FROM dbo.OrderItem WHERE OrderID=100 ORDER BY ID").ToList() }
        });
        var connections = new Mock<IDbConnectionFactory>();
        connections.Setup(x => x.NewSqlConnection()).Returns(() => new SqlConnection(connectionString));
        return new OrderService(sales.Object, clients.Object, Mock.Of<IUserRepositoriesFactory>(), products.Object,
            Mock.Of<IAgreementRepositoriesFactory>(), Mock.Of<IStorageRepositoryFactory>(), rates.Object,
            Mock.Of<ICurrencyRepositoriesFactory>(), Mock.Of<IDeliveryRepositoriesFactory>(), Mock.Of<IRetailClientRepositoriesFactory>(),
            connections.Object, Mock.Of<IPaymentLinkService>(), new RecordingCrmHandler().Factory(),
            Mock.Of<IClientResourceAccessService>(), Mock.Of<ISearchReindexSignal>(), new CheckoutExecutor(connections.Object, new SqlCheckoutLedger()));
    }

    private const string Schema = """
        DROP TABLE IF EXISTS dbo.ProductReservation,dbo.ProductAvailability,dbo.OrderItem,dbo.ClientShoppingCart,dbo.Product,dbo.ClientAgreement,dbo.Client;
        CREATE TABLE dbo.Client(ID bigint PRIMARY KEY,NetUID uniqueidentifier);
        CREATE TABLE dbo.ClientAgreement(ID bigint PRIMARY KEY,NetUID uniqueidentifier,ClientID bigint);
        CREATE TABLE dbo.Product(ID bigint PRIMARY KEY,NetUID uniqueidentifier);
        CREATE TABLE dbo.ClientShoppingCart(ID bigint PRIMARY KEY,Deleted bit NOT NULL,IsOffer bit NOT NULL,
            ClientAgreementID bigint,IsVatCart bit,WorkplaceID bigint NULL);
        CREATE TABLE dbo.OrderItem(ID bigint IDENTITY PRIMARY KEY,NetUID uniqueidentifier NOT NULL DEFAULT NEWID(),ProductID bigint,Qty float,
            PricePerItem decimal(18,2),OrderID bigint NULL,ClientShoppingCartID bigint NULL,Deleted bit NOT NULL DEFAULT 0,Updated datetime);
        CREATE TABLE dbo.ProductAvailability(ID bigint PRIMARY KEY,ProductID bigint,Amount float,Deleted bit);
        CREATE INDEX IX_ProductAvailability_ProductID ON dbo.ProductAvailability(ProductID);
        CREATE TABLE dbo.ProductReservation(ID bigint PRIMARY KEY,OrderItemID bigint,Qty float);
        INSERT dbo.Client VALUES(1,NEWID());
        INSERT dbo.ClientAgreement VALUES(20,NEWID(),1);
        INSERT dbo.Product VALUES(7,NEWID());
        INSERT dbo.ClientShoppingCart VALUES(10,0,0,20,0,NULL);
        INSERT dbo.OrderItem(NetUID,ProductID,Qty,PricePerItem,OrderID,ClientShoppingCartID,Deleted,Updated)
            VALUES(NEWID(),7,2,9,NULL,10,0,GETUTCDATE()),(NEWID(),7,5,9,NULL,10,0,GETUTCDATE());
        INSERT dbo.ProductAvailability VALUES(1,7,10,0);
        INSERT dbo.ProductReservation VALUES(1,1,2),(2,2,5);
        ALTER TABLE dbo.OrderItem ADD InvoiceDocumentQty float NULL,UserID bigint NULL,Comment nvarchar(450) NULL,
            IsValidForCurrentSale bit NULL,ExchangeRateAmount decimal(30,14) NULL,DiscountAmount decimal(30,14) NULL,
            PricePerItemWithoutVat decimal(30,14) NULL,Vat decimal(30,14) NULL,IsClosed bit NULL;
        """;
}

public sealed class CheckoutCartSqlFactAttribute : FactAttribute {
    public CheckoutCartSqlFactAttribute() {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(CheckoutSqlFixture.ConnectionStringVariable)))
            Skip = "A disposable checkout SQL database is required.";
    }
}
