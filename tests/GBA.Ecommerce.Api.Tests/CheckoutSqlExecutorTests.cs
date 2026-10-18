using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Products;
using GBA.Services.Infrastructure.Checkout;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Api.Tests;

[Collection(CheckoutSqlCollection.Name)]
public sealed class CheckoutSqlExecutorTests(CheckoutSqlFixture fixture) {
    [Fact]
    public void Checkout_writes_and_its_receipt_commit_together_and_replay_without_new_writes() {
        if (!fixture.IsConfigured) return;

        CheckoutRequest request = CheckoutExecutorTests.NewRequest();
        CheckoutExecutor executor = NewExecutor();
        int created = 0;

        CheckoutResult first = executor.Execute(request, connection => {
            created++;
            return InsertProbeSale(connection, request.OperationNetUid);
        });
        CheckoutResult second = executor.Execute(request, connection => {
            created++;
            return InsertProbeSale(connection, request.OperationNetUid);
        });

        Assert.False(first.IsReplay);
        Assert.True(second.IsReplay);
        Assert.Equal(first.Sale.NetUid, second.Sale.NetUid);
        Assert.Equal(first.Sale.Id, second.Sale.Id);
        Assert.Equal(1, created);
        Assert.Equal(1, ProbeRows(request.OperationNetUid));
        Assert.True(ReceiptCompleted(request.OperationNetUid));
    }

    [Fact]
    public void A_failure_after_writes_rolls_back_the_writes_and_leaves_the_key_reusable() {
        if (!fixture.IsConfigured) return;

        CheckoutRequest request = CheckoutExecutorTests.NewRequest();
        CheckoutExecutor executor = NewExecutor();
        bool effectRan = false;

        Assert.Throws<InvalidOperationException>(() => executor.Execute(request, connection => {
            InsertProbeSale(connection, request.OperationNetUid);
            TransactionalEffects.AfterCommit(() => effectRan = true);
            throw new InvalidOperationException("CRM endpoint is not configured.");
        }));

        Assert.Equal(0, ProbeRows(request.OperationNetUid));
        Assert.False(ReceiptExists(request.OperationNetUid));
        Assert.False(effectRan);

        CheckoutResult retried = executor.Execute(request, connection => InsertProbeSale(connection, request.OperationNetUid));

        Assert.False(retried.IsReplay);
        Assert.Equal(1, ProbeRows(request.OperationNetUid));
    }

    [Fact]
    public async Task Concurrent_submissions_with_one_key_create_exactly_one_sale() {
        if (!fixture.IsConfigured) return;

        CheckoutRequest request = CheckoutExecutorTests.NewRequest();
        int created = 0;

        CheckoutResult[] results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(() =>
            NewExecutor().Execute(request, connection => {
                Interlocked.Increment(ref created);
                Sale sale = InsertProbeSale(connection, request.OperationNetUid);
                Thread.Sleep(300);
                return sale;
            }))));

        Assert.Equal(1, created);
        Assert.Equal(1, ProbeRows(request.OperationNetUid));
        Assert.Single(results, result => !result.IsReplay);
        Assert.All(results, result => Assert.Equal(results[0].Sale.NetUid, result.Sale.NetUid));
    }

    [Fact]
    public async Task Checkouts_with_different_keys_run_concurrently() {
        if (!fixture.IsConfigured) return;

        using Barrier bothInside = new(2);
        CheckoutRequest first = CheckoutExecutorTests.NewRequest();
        CheckoutRequest second = CheckoutExecutorTests.NewRequest();

        Func<CheckoutRequest, Task<CheckoutResult>> run = request => Task.Run(() =>
            NewExecutor().Execute(request, connection => {
                Sale sale = InsertProbeSale(connection, request.OperationNetUid);
                if (!bothInside.SignalAndWait(TimeSpan.FromSeconds(15)))
                    throw new TimeoutException("The checkouts did not overlap.");
                return sale;
            }));

        CheckoutResult[] results = await Task.WhenAll(run(first), run(second));

        Assert.All(results, result => Assert.False(result.IsReplay));
        Assert.Equal(1, ProbeRows(first.OperationNetUid));
        Assert.Equal(1, ProbeRows(second.OperationNetUid));
    }

    [Fact]
    public async Task A_key_held_by_a_running_checkout_is_reported_as_in_progress_and_replays_after_commit() {
        if (!fixture.IsConfigured) return;

        CheckoutRequest request = CheckoutExecutorTests.NewRequest();
        using ManualResetEventSlim inside = new();
        using ManualResetEventSlim release = new();

        Task<CheckoutResult> running = Task.Run(() => NewExecutor().Execute(request, connection => {
            Sale sale = InsertProbeSale(connection, request.OperationNetUid);
            inside.Set();
            release.Wait(TimeSpan.FromSeconds(20));
            return sale;
        }));

        Assert.True(inside.Wait(TimeSpan.FromSeconds(15)));

        CheckoutExecutor impatient = NewExecutor(new SqlCheckoutLedger(300));
        CheckoutConflictException conflict = Assert.Throws<CheckoutConflictException>(() =>
            impatient.Execute(request, connection => InsertProbeSale(connection, request.OperationNetUid)));
        Assert.Equal(CheckoutConflictException.InProgressMessage, conflict.Message);

        release.Set();
        CheckoutResult committed = await running;
        CheckoutResult replay = impatient.Execute(request, connection => InsertProbeSale(connection, request.OperationNetUid));

        Assert.True(replay.IsReplay);
        Assert.Equal(committed.Sale.NetUid, replay.Sale.NetUid);
        Assert.Equal(1, ProbeRows(request.OperationNetUid));
    }

    [Fact]
    public void Post_commit_effects_run_after_a_real_sql_commit() {
        if (!fixture.IsConfigured) return;

        CheckoutRequest request = CheckoutExecutorTests.NewRequest();
        bool visibleWhenEffectRan = false;

        NewExecutor().Execute(request, connection => {
            Sale sale = InsertProbeSale(connection, request.OperationNetUid);
            TransactionalEffects.AfterCommit(() => visibleWhenEffectRan = ProbeRows(request.OperationNetUid) == 1);
            return sale;
        });

        Assert.True(visibleWhenEffectRan);
    }

    [Fact]
    public async Task Stock_locks_prevent_lost_updates_between_concurrent_checkouts() {
        if (!fixture.IsConfigured || !fixture.HasConcordSchema) return;

        (long productId, long storageId, double original) = PickAvailability();

        try {
            await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() => {
                using TransactionScope scope = CheckoutTransaction.Begin();
                using IDbConnection connection = fixture.ConnectionFactory.NewSqlConnection();
                connection.Open();
                CheckoutLocks.LockProductStock(connection, new[] { productId });
                ProductAvailabilityRepository repository = new(connection);
                ProductAvailability availability = repository.GetByProductAndStorageIds(productId, storageId);
                Thread.Sleep(300);
                availability.Amount -= 1;
                repository.Update(availability);
                scope.Complete();
            })));

            using SqlConnection verify = fixture.Open();
            double amount = verify.ExecuteScalar<double>(
                "SELECT TOP (1) Amount FROM dbo.ProductAvailability WHERE ProductID = @productId AND StorageID = @storageId AND Deleted = 0 ORDER BY ID",
                new { productId, storageId });
            Assert.Equal(original - 2, amount);
        } finally {
            using SqlConnection restore = fixture.Open();
            restore.Execute(
                "UPDATE dbo.ProductAvailability SET Amount = @original WHERE ID = (SELECT TOP (1) ID FROM dbo.ProductAvailability WHERE ProductID = @productId AND StorageID = @storageId AND Deleted = 0 ORDER BY ID)",
                new { original, productId, storageId });
        }
    }

    private (long ProductId, long StorageId, double Amount) PickAvailability() {
        using SqlConnection connection = fixture.Open();
        return connection.QuerySingle<(long, long, double)>("""
            SELECT TOP (1) ProductID, StorageID, Amount
            FROM dbo.ProductAvailability
            WHERE Deleted = 0 AND Amount >= 10
            ORDER BY ID DESC;
            """);
    }

    private CheckoutExecutor NewExecutor(ICheckoutLedger? ledger = null) {
        return new CheckoutExecutor(fixture.ConnectionFactory, ledger ?? new SqlCheckoutLedger());
    }

    private static Sale InsertProbeSale(IDbConnection connection, Guid operationNetUid) {
        CheckoutTransaction.RequireAmbient();
        long id = connection.ExecuteScalar<long>(
            "INSERT INTO dbo.CheckoutAtomicityProbe (OperationNetUid, Note) VALUES (@operationNetUid, N'sale'); SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            new { operationNetUid });
        return new Sale { Id = id, NetUid = Guid.NewGuid(), SaleNumber = new SaleNumber { Value = $"P{id}" } };
    }

    private int ProbeRows(Guid operationNetUid) {
        using TransactionScope suppressed = new(TransactionScopeOption.Suppress);
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM dbo.CheckoutAtomicityProbe WHERE OperationNetUid = @operationNetUid",
            new { operationNetUid });
    }

    private bool ReceiptExists(Guid operationNetUid) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM dbo.EcommerceSalesCreationLedger WHERE OperationNetUid = @operationNetUid",
            new { operationNetUid }) == 1;
    }

    private bool ReceiptCompleted(Guid operationNetUid) {
        using SqlConnection connection = fixture.Open();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM dbo.EcommerceSalesCreationLedger WHERE OperationNetUid = @operationNetUid AND CompletedUtc IS NOT NULL AND ISJSON(ResponsePayload) = 1",
            new { operationNetUid }) == 1;
    }
}
