using System.Transactions;
using GBA.Services.Infrastructure;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class BackgroundSyncRunnerTransactionTests {
    [Fact]
    public async Task NotificationWaitsForCommitAndDoesNotInheritTheTransaction() {
        TaskCompletionSource<Transaction?> invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)) {
            BackgroundSyncRunner.Run(_ => {
                invoked.TrySetResult(Transaction.Current);
                return Task.CompletedTask;
            }, "test notification");
            Assert.False(invoked.Task.IsCompleted);
            transaction.Complete();
        }
        Assert.Null(await invoked.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void RolledBackMutationNeverStartsNotification() {
        int count = 0;
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)) {
            BackgroundSyncRunner.Run(_ => {
                Interlocked.Increment(ref count);
                return Task.CompletedTask;
            }, "rollback notification");
        }
        Assert.Equal(0, Volatile.Read(ref count));
    }
}
