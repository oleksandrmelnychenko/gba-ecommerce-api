using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Transactions;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;
using GBA.Services.Infrastructure.Checkout;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class CheckoutExecutorTests {
    [Fact]
    public void A_completed_receipt_is_replayed_without_running_the_checkout_again() {
        FakeLedger ledger = new();
        CheckoutRequest request = NewRequest();
        Sale stored = NewSale();
        ledger.Store(request, stored);
        int created = 0;

        CheckoutResult result = NewExecutor(ledger).Execute(request, _ => {
            created++;
            return NewSale();
        });

        Assert.True(result.IsReplay);
        Assert.Equal(stored.NetUid, result.Sale.NetUid);
        Assert.Equal(0, created);
        Assert.Equal(0, ledger.ClaimCalls);
    }

    [Fact]
    public void A_key_reused_for_a_different_request_is_rejected_without_running_the_checkout() {
        FakeLedger ledger = new();
        CheckoutRequest original = NewRequest();
        ledger.Store(original, NewSale());
        CheckoutRequest changed = new(
            original.OperationName,
            new CheckoutAttempt(original.OperationNetUid, CheckoutFingerprint.Compute("changed")),
            original.PrincipalNetUid,
            original.ClientNetUid,
            original.ModeFlag);
        int created = 0;

        Assert.Throws<CheckoutKeyReuseException>(() => NewExecutor(ledger).Execute(changed, _ => {
            created++;
            return NewSale();
        }));
        Assert.Equal(0, created);
    }

    [Fact]
    public void A_new_checkout_runs_once_inside_its_own_transaction_and_stores_the_response() {
        FakeLedger ledger = new();
        CheckoutRequest request = NewRequest();
        Sale sale = NewSale();
        System.Transactions.IsolationLevel? observedIsolation = null;

        CheckoutResult result = NewExecutor(ledger).Execute(request, _ => {
            observedIsolation = Transaction.Current?.IsolationLevel;
            return sale;
        });

        Assert.False(result.IsReplay);
        Assert.Same(sale, result.Sale);
        Assert.Equal(System.Transactions.IsolationLevel.ReadCommitted, observedIsolation);
        Assert.Null(Transaction.Current);
        Assert.Equal(1, ledger.ClaimCalls);
        string payload = Assert.Single(ledger.CompletedPayloads);
        Assert.Equal(sale.NetUid, JsonSerializer.Deserialize<Sale>(payload, CheckoutExecutor.ReceiptJsonOptions)!.NetUid);
    }

    [Fact]
    public void A_failed_checkout_completes_no_receipt_and_runs_no_post_commit_effect() {
        FakeLedger ledger = new();
        bool effectRan = false;

        Assert.Throws<ArgumentException>(() => NewExecutor(ledger).Execute(NewRequest(), _ => {
            TransactionalEffects.AfterCommit(() => effectRan = true);
            throw new ArgumentException("The current shopping cart is empty.");
        }));

        Assert.Empty(ledger.CompletedPayloads);
        Assert.False(effectRan);
        Assert.Null(Transaction.Current);
    }

    [Fact]
    public void Post_commit_effects_registered_by_the_checkout_run_only_after_it_commits() {
        FakeLedger ledger = new();
        bool effectRanInsideCheckout = false;
        bool effectRan = false;

        NewExecutor(ledger).Execute(NewRequest(), _ => {
            TransactionalEffects.AfterCommit(() => effectRan = true);
            effectRanInsideCheckout = effectRan;
            return NewSale();
        });

        Assert.False(effectRanInsideCheckout);
        Assert.True(effectRan);
    }

    [Fact]
    public void A_sale_that_could_not_be_reloaded_rolls_the_checkout_back() {
        FakeLedger ledger = new();

        Assert.Throws<InvalidOperationException>(() =>
            NewExecutor(ledger).Execute(NewRequest(), _ => new Sale()));

        Assert.Empty(ledger.CompletedPayloads);
    }

    [Fact]
    public void A_key_already_committed_by_a_concurrent_request_returns_that_result() {
        FakeLedger ledger = new();
        CheckoutRequest request = NewRequest();
        Sale winner = NewSale();
        ledger.OnClaim = () => {
            ledger.Store(request, winner);
            return CheckoutClaim.AlreadyRecorded;
        };
        int created = 0;

        CheckoutResult result = NewExecutor(ledger).Execute(request, _ => {
            created++;
            return NewSale();
        });

        Assert.True(result.IsReplay);
        Assert.Equal(winner.NetUid, result.Sale.NetUid);
        Assert.Equal(0, created);
    }

    [Fact]
    public void A_key_still_held_by_a_concurrent_request_is_reported_as_in_progress() {
        FakeLedger ledger = new() { OnClaim = () => CheckoutClaim.Busy };
        int created = 0;

        CheckoutConflictException conflict = Assert.Throws<CheckoutConflictException>(() =>
            NewExecutor(ledger).Execute(NewRequest(), _ => {
                created++;
                return NewSale();
            }));

        Assert.Equal(CheckoutConflictException.InProgressMessage, conflict.Message);
        Assert.Equal(0, created);
    }

    [Fact]
    public void An_incomplete_receipt_is_never_replayed() {
        FakeLedger ledger = new();
        CheckoutRequest request = NewRequest();
        ledger.Store(request, null);

        Assert.Throws<CheckoutConflictException>(() => NewExecutor(ledger).FindReplay(request));
    }

    [Fact]
    public void Checkout_refuses_to_join_a_transaction_it_does_not_own() {
        using TransactionScope ambient = new();

        Assert.Throws<InvalidOperationException>(() =>
            NewExecutor(new FakeLedger()).Execute(NewRequest(), _ => NewSale()));
    }

    internal static CheckoutRequest NewRequest() {
        Guid principal = Guid.NewGuid();
        return new CheckoutRequest(
            "test-checkout",
            new CheckoutAttempt(Guid.NewGuid(), CheckoutFingerprint.ForCart(false)),
            principal,
            principal,
            false);
    }

    internal static Sale NewSale() {
        return new Sale {
            Id = Random.Shared.Next(1, int.MaxValue),
            NetUid = Guid.NewGuid(),
            SaleNumber = new SaleNumber { Value = "T00000001" }
        };
    }

    private static CheckoutExecutor NewExecutor(ICheckoutLedger ledger) {
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(() => new Mock<IDbConnection>().Object);

        return new CheckoutExecutor(connectionFactory.Object, ledger);
    }

    private sealed class FakeLedger : ICheckoutLedger {
        private readonly Dictionary<Guid, CheckoutLedgerEntry> _entries = new();

        public Func<CheckoutClaim>? OnClaim { get; set; }

        public int ClaimCalls { get; private set; }

        public List<string> CompletedPayloads { get; } = new();

        public void Store(CheckoutRequest request, Sale? sale) {
            _entries[request.OperationNetUid] = new CheckoutLedgerEntry {
                OperationNetUid = request.OperationNetUid,
                OperationName = request.OperationName,
                PrincipalNetUid = request.PrincipalNetUid,
                ClientNetUid = request.ClientNetUid,
                ModeFlag = request.ModeFlag,
                RequestFingerprint = request.RequestFingerprint,
                ResponsePayload = sale == null ? null : JsonSerializer.Serialize(sale, CheckoutExecutor.ReceiptJsonOptions),
                CreatedUtc = DateTime.UtcNow,
                CompletedUtc = sale == null ? null : DateTime.UtcNow
            };
        }

        public CheckoutLedgerEntry Find(IDbConnection connection, Guid operationNetUid) {
            return _entries.GetValueOrDefault(operationNetUid)!;
        }

        public CheckoutClaim TryClaim(IDbConnection connection, CheckoutRequest request) {
            ClaimCalls++;
            Assert.NotNull(Transaction.Current);
            return OnClaim?.Invoke() ?? CheckoutClaim.Claimed;
        }

        public void Complete(IDbConnection connection, CheckoutRequest request, string responsePayload) {
            Assert.NotNull(Transaction.Current);
            CompletedPayloads.Add(responsePayload);
        }
    }
}
