using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using GBA.Common.Helpers;
using GBA.Common.Search;
using GBA.Services.Infrastructure;

namespace GBA.Ecommerce.Api.Tests;

public sealed class TransactionalEffectsTests {
    [Fact]
    public void Effects_outside_a_transaction_run_immediately() {
        bool ran = false;

        TransactionalEffects.AfterCommit(() => ran = true);

        Assert.True(ran);
    }

    [Fact]
    public void Effects_inside_a_transaction_wait_for_the_commit() {
        bool ran = false;

        using (TransactionScope scope = new()) {
            TransactionalEffects.AfterCommit(() => ran = true);
            Assert.False(ran);
            scope.Complete();
        }

        Assert.True(ran);
    }

    [Fact]
    public void Effects_of_a_rolled_back_transaction_never_run() {
        bool ran = false;

        using (new TransactionScope()) {
            TransactionalEffects.AfterCommit(() => ran = true);
        }

        Assert.False(ran);
    }

    [Fact]
    public void Effects_do_not_inherit_the_completed_transaction() {
        Transaction? observed = new CommittableTransaction();

        using (TransactionScope scope = new()) {
            TransactionalEffects.AfterCommit(() => observed = Transaction.Current);
            scope.Complete();
        }

        Assert.Null(observed);
    }

    [Fact]
    public void A_failing_effect_does_not_fail_the_committed_transaction() {
        using TransactionScope scope = new();
        TransactionalEffects.AfterCommit(() => throw new InvalidOperationException("notification failed"));
        scope.Complete();
    }

    [Fact]
    public void Reindex_requests_inside_a_rolled_back_transaction_are_dropped() {
        SearchReindexSignal signal = new();

        using (new TransactionScope()) {
            signal.Request(17);
        }

        Assert.False(signal.Reader.TryRead(out _));
    }

    [Fact]
    public void Reindex_requests_inside_a_committed_transaction_are_published_after_commit() {
        SearchReindexSignal signal = new();

        using (TransactionScope scope = new()) {
            signal.Request(17);
            Assert.False(signal.Reader.TryRead(out _));
            scope.Complete();
        }

        Assert.True(signal.Reader.TryRead(out long productId));
        Assert.Equal(17, productId);
    }

    [Fact]
    public async Task Background_sync_inside_a_rolled_back_transaction_never_starts() {
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)) {
            BackgroundSyncRunner.Run(_ => {
                started.TrySetResult();
                return Task.CompletedTask;
            }, "rolled back sync");
        }

        Task completed = await Task.WhenAny(started.Task, Task.Delay(TimeSpan.FromMilliseconds(500)));
        Assert.NotSame(started.Task, completed);
    }

    [Fact]
    public async Task Background_sync_inside_a_committed_transaction_starts_after_commit_without_the_transaction() {
        TaskCompletionSource<Transaction?> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool startedBeforeCommit;

        using (TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled)) {
            BackgroundSyncRunner.Run(_ => {
                started.TrySetResult(Transaction.Current);
                return Task.CompletedTask;
            }, "committed sync");

            await Task.Delay(TimeSpan.FromMilliseconds(200));
            startedBeforeCommit = started.Task.IsCompleted;
            scope.Complete();
        }

        Transaction? backgroundTransaction = await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(startedBeforeCommit);
        Assert.Null(backgroundTransaction);
    }

    [Fact]
    public async Task Background_sync_outside_a_transaction_starts_immediately() {
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        BackgroundSyncRunner.Run(_ => {
            started.TrySetResult();
            return Task.CompletedTask;
        }, "immediate sync");

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
