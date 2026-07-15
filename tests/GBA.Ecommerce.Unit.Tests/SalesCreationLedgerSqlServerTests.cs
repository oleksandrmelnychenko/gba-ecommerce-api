using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using Dapper;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Services.Infrastructure.SalesMutations;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Unit.Tests;

[Collection("EcommerceSqlIntegration")]
public sealed class SalesCreationLedgerSqlServerTests {
    private static readonly DateTime _createdUtc =
        new(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _completedUtc =
        new(2026, 7, 15, 8, 0, 1, DateTimeKind.Utc);

    [Fact]
    public async Task SameKeyAndExactRequestReplaysPermanentReceipt() {
        string? connectionString = GetConnectionString();
        if (connectionString == null) return;

        SqlSalesCreationLedgerStore store = await CreateStoreAsync(connectionString);
        SalesCreationRequest request = CreateRequest(Guid.NewGuid());
        const string response = "{\"NetUid\":\"95483133-e651-4d4a-aac0-b0792142b37b\"}";
        await DeleteAsync(connectionString, request.OperationNetUid);

        try {
            await RegisterAndCompleteAsync(store, connectionString, request, response);

            SalesCreationLedgerRegistration replayRegistration =
                await RegisterAsync(store, connectionString, request);
            SalesCreationLedgerEntry? replay = await store.GetAsync(
                request.OperationNetUid,
                CancellationToken.None);

            Assert.False(replayRegistration.WasInserted);
            Assert.NotNull(replay);
            SalesCreationRequestKey.EnsureMatches(request, replay);
            Assert.Equal(response, replay.ResponsePayload);
            Assert.Equal(_completedUtc, replay.CompletedUtc);
        } finally {
            await DeleteAsync(connectionString, request.OperationNetUid);
        }
    }

    [Fact]
    public async Task SameKeyWithDifferentPrincipalModeOrPayloadReturnsConflict() {
        string? connectionString = GetConnectionString();
        if (connectionString == null) return;

        SqlSalesCreationLedgerStore store = await CreateStoreAsync(connectionString);
        Guid operationNetUid = Guid.NewGuid();
        SalesCreationRequest original = CreateRequest(operationNetUid);
        SalesCreationRequest mismatch = SalesCreationRequestKey.Create(
            operationNetUid,
            original.OperationName,
            Guid.NewGuid(),
            original.ClientNetUid,
            true,
            CreateSale(3d));
        await DeleteAsync(connectionString, operationNetUid);

        try {
            await RegisterAndCompleteAsync(
                store,
                connectionString,
                original,
                "{\"NetUid\":\"95483133-e651-4d4a-aac0-b0792142b37b\"}");

            await Assert.ThrowsAsync<SalesCreationIdempotencyConflictException>(() =>
                RegisterAsync(store, connectionString, mismatch));

            SalesCreationLedgerEntry? persisted = await store.GetAsync(
                operationNetUid,
                CancellationToken.None);
            Assert.NotNull(persisted);
            SalesCreationRequestKey.EnsureMatches(original, persisted);
        } finally {
            await DeleteAsync(connectionString, operationNetUid);
        }
    }

    [Fact]
    public async Task ConcurrentSameKeyHasOneOwnerAndOneExactReplay() {
        string? connectionString = GetConnectionString();
        if (connectionString == null) return;

        SqlSalesCreationLedgerStore store = await CreateStoreAsync(connectionString);
        SalesCreationRequest request = CreateRequest(Guid.NewGuid());
        const string response = "{\"NetUid\":\"8ad11dc5-891f-46d6-a85c-2dca5d69f882\"}";
        await DeleteAsync(connectionString, request.OperationNetUid);

        TaskCompletionSource firstReserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        try {
            Task first = Task.Run(async () => {
                using IDbConnection connection = new SqlConnection(connectionString);
                using TransactionScope transaction = CreateTransactionScope();
                SalesCreationLedgerRegistration registration =
                    await store.RegisterAsync(connection, request, _createdUtc, CancellationToken.None);
                Assert.True(registration.WasInserted);
                firstReserved.SetResult();
                await releaseFirst.Task;
                await store.CompleteAsync(
                    connection,
                    request,
                    response,
                    _completedUtc,
                    CancellationToken.None);
                transaction.Complete();
            });

            await firstReserved.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Task<SalesCreationLedgerRegistration> concurrent = Task.Run(() =>
                RegisterAsync(store, connectionString, request));
            SalesCreationRequest concurrentMismatch = SalesCreationRequestKey.Create(
                request.OperationNetUid,
                request.OperationName,
                request.PrincipalNetUid,
                request.ClientNetUid,
                true,
                CreateSale(3d));
            Task<SalesCreationIdempotencyConflictException> mismatchedConcurrent =
                Assert.ThrowsAsync<SalesCreationIdempotencyConflictException>(() =>
                    Task.Run(() => RegisterAsync(
                        store,
                        connectionString,
                        concurrentMismatch)));

            await Task.Delay(250);
            Assert.False(concurrent.IsCompleted);
            Assert.False(mismatchedConcurrent.IsCompleted);

            releaseFirst.SetResult();
            await first.WaitAsync(TimeSpan.FromSeconds(10));
            SalesCreationLedgerRegistration replay =
                await concurrent.WaitAsync(TimeSpan.FromSeconds(10));
            await mismatchedConcurrent.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.False(replay.WasInserted);
            Assert.Equal(response, replay.Entry.ResponsePayload);
        } finally {
            releaseFirst.TrySetResult();
            await DeleteAsync(connectionString, request.OperationNetUid);
        }
    }

    [Fact]
    public async Task IdenticalPurchasesWithDifferentExplicitKeysCreateDifferentReceipts() {
        string? connectionString = GetConnectionString();
        if (connectionString == null) return;

        SqlSalesCreationLedgerStore store = await CreateStoreAsync(connectionString);
        Guid firstKey = Guid.NewGuid();
        Guid secondKey = Guid.NewGuid();
        SalesCreationRequest first = CreateRequest(firstKey);
        SalesCreationRequest second = CreateRequest(secondKey);
        await DeleteAsync(connectionString, firstKey, secondKey);

        try {
            Assert.Equal(first.RequestFingerprint, second.RequestFingerprint);

            await RegisterAndCompleteAsync(
                store,
                connectionString,
                first,
                "{\"NetUid\":\"d11fd72f-b953-49b7-b53a-fc52aa73304f\"}");
            await RegisterAndCompleteAsync(
                store,
                connectionString,
                second,
                "{\"NetUid\":\"382010b2-6691-4d9a-84fb-cae47e54a239\"}");

            await using SqlConnection connection = new(connectionString);
            int count = await connection.ExecuteScalarAsync<int>(
                """
SELECT COUNT(*)
FROM dbo.EcommerceSalesCreationLedger
WHERE OperationNetUid IN @OperationNetUids;
""",
                new { OperationNetUids = new[] { firstKey, secondKey } });
            Assert.Equal(2, count);
        } finally {
            await DeleteAsync(connectionString, firstKey, secondKey);
        }
    }

    [Fact]
    public async Task DispatchCleanupDoesNotDeletePermanentCreationReceipt() {
        string? connectionString = GetConnectionString();
        if (connectionString == null) return;

        TestDbConnectionFactory factory = new(connectionString);
        SqlSalesCreationLedgerStore ledgerStore = new(factory);
        SqlSalesMutationOutboxStore outboxStore = new(factory);
        await ledgerStore.EnsureSchemaAsync(CancellationToken.None);
        await outboxStore.EnsureSchemaAsync(CancellationToken.None);
        SalesCreationRequest request = CreateRequest(Guid.NewGuid());
        const string response = "{\"NetUid\":\"9f08738f-c6de-4ab9-b13b-ab8532805122\"}";
        string outboxPayload =
            $"{{\"NetUid\":\"9f08738f-c6de-4ab9-b13b-ab8532805122\",\"OperationNetUid\":\"{request.OperationNetUid:D}\"}}";
        await DeleteAsync(connectionString, request.OperationNetUid);
        await DeleteOutboxAsync(connectionString, request.OperationNetUid);

        try {
            await RegisterAndCompleteAsync(
                ledgerStore,
                connectionString,
                request,
                response);
            await outboxStore.EnqueueAsync(new SalesMutationOutboxMessage {
                OperationNetUid = request.OperationNetUid,
                OperationName = request.OperationName,
                RequestUrl = "https://crm.example/api/v1/uk/sales/update/ecommerce",
                Payload = outboxPayload,
                PayloadSha256 = SHA256.HashData(Encoding.UTF8.GetBytes(outboxPayload)),
                Status = SalesMutationOutboxStatus.Pending,
                AttemptCount = 0,
                NextAttemptUtc = _createdUtc,
                CreatedUtc = _createdUtc
            }, CancellationToken.None);
            SalesMutationOutboxLease lease = await outboxStore.ClaimNextAsync(
                _createdUtc,
                TimeSpan.FromMinutes(1),
                CancellationToken.None);
            Assert.Equal(request.OperationNetUid, lease.OperationNetUid);
            Assert.True(await outboxStore.CompleteAsync(
                request.OperationNetUid,
                lease.LeaseToken,
                _completedUtc,
                CancellationToken.None));

            Assert.Equal(1, await outboxStore.DeleteCompletedBeforeAsync(
                _completedUtc.AddSeconds(1),
                CancellationToken.None));
            Assert.NotNull(await ledgerStore.GetAsync(
                request.OperationNetUid,
                CancellationToken.None));
        } finally {
            await DeleteOutboxAsync(connectionString, request.OperationNetUid);
            await DeleteAsync(connectionString, request.OperationNetUid);
        }
    }

    private static async Task<SqlSalesCreationLedgerStore> CreateStoreAsync(
        string connectionString) {
        SqlSalesCreationLedgerStore store = new(new TestDbConnectionFactory(connectionString));
        await store.EnsureSchemaAsync(CancellationToken.None);
        return store;
    }

    private static async Task RegisterAndCompleteAsync(
        SqlSalesCreationLedgerStore store,
        string connectionString,
        SalesCreationRequest request,
        string responsePayload) {
        using IDbConnection connection = new SqlConnection(connectionString);
        using TransactionScope transaction = CreateTransactionScope();
        SalesCreationLedgerRegistration registration = await store.RegisterAsync(
            connection,
            request,
            _createdUtc,
            CancellationToken.None);
        Assert.True(registration.WasInserted);
        await store.CompleteAsync(
            connection,
            request,
            responsePayload,
            _completedUtc,
            CancellationToken.None);
        transaction.Complete();
    }

    private static async Task<SalesCreationLedgerRegistration> RegisterAsync(
        SqlSalesCreationLedgerStore store,
        string connectionString,
        SalesCreationRequest request) {
        using IDbConnection connection = new SqlConnection(connectionString);
        using TransactionScope transaction = CreateTransactionScope();
        SalesCreationLedgerRegistration registration = await store.RegisterAsync(
            connection,
            request,
            _createdUtc,
            CancellationToken.None);
        transaction.Complete();
        return registration;
    }

    private static TransactionScope CreateTransactionScope() =>
        new(
            TransactionScopeOption.Required,
            new TransactionOptions {
                IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromMinutes(1)
            },
            TransactionScopeAsyncFlowOption.Enabled);

    private static SalesCreationRequest CreateRequest(Guid operationNetUid) {
        Guid principalNetUid = Guid.Parse("d184878d-7a4a-487f-9e28-ac2cc40d7bd1");
        return SalesCreationRequestKey.Create(
            operationNetUid,
            SalesMutationOperationNames.RetailSaleUpdate,
            principalNetUid,
            principalNetUid,
            false,
            CreateSale(2d));
    }

    private static Sale CreateSale(double qty) => new() {
        Comment = "identical checkout",
        Order = new Order {
            OrderItems = {
                new OrderItem {
                    ProductId = 17,
                    Qty = qty,
                    Product = new Product {
                        Id = 17,
                        NetUid = Guid.Parse("00000000-0000-0000-0000-000000000017")
                    }
                }
            }
        }
    };

    private static string? GetConnectionString() {
        string? connectionString = Environment.GetEnvironmentVariable(
            "GBA_ECOMMERCE_SQL_INTEGRATION_CONNECTION_STRING");
        bool required = string.Equals(
            Environment.GetEnvironmentVariable("GBA_ECOMMERCE_SQL_INTEGRATION_REQUIRED"),
            "1",
            StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(connectionString) && required)
            throw new InvalidOperationException(
                "GBA_ECOMMERCE_SQL_INTEGRATION_CONNECTION_STRING is required for this test run.");
        return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString;
    }

    private static async Task DeleteAsync(
        string connectionString,
        params Guid[] operationNetUids) {
        await using SqlConnection connection = new(connectionString);
        await connection.ExecuteAsync(
            """
IF OBJECT_ID(N'dbo.EcommerceSalesCreationLedger', N'U') IS NOT NULL
    DELETE FROM dbo.EcommerceSalesCreationLedger
    WHERE OperationNetUid IN @OperationNetUids;
""",
            new { OperationNetUids = operationNetUids });
    }

    private static async Task DeleteOutboxAsync(
        string connectionString,
        Guid operationNetUid) {
        await using SqlConnection connection = new(connectionString);
        await connection.ExecuteAsync(
            """
IF OBJECT_ID(N'dbo.EcommerceSalesMutationOutbox', N'U') IS NOT NULL
    DELETE FROM dbo.EcommerceSalesMutationOutbox
    WHERE OperationNetUid = @OperationNetUid;
""",
            new { OperationNetUid = operationNetUid });
    }

    private sealed class TestDbConnectionFactory(string connectionString) : IDbConnectionFactory {
        public IDbConnection NewSqlConnection() => new SqlConnection(connectionString);
        public IDbConnection NewDataAnalyticSqlConnection() => throw new NotSupportedException();
        public IDbConnection NewIdentitySqlConnection() => throw new NotSupportedException();
        public IDbConnection NewFenixOneCSqlConnection() => throw new NotSupportedException();
        public IDbConnection NewAmgOneCSqlConnection() => throw new NotSupportedException();
    }
}
