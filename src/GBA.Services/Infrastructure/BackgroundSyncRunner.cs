using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NLog;

namespace GBA.Services.Infrastructure;

// Best-effort work only. Business mutations that must survive process restarts belong in a durable outbox.
public static class BackgroundSyncRunner {
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(15);

    public static void Run(
        Func<CancellationToken, Task> action,
        string operationName,
        TimeSpan? timeout = null) {
        if (Transaction.Current is { } transaction) {
            transaction.TransactionCompleted += (_, completed) => {
                if (completed.Transaction?.TransactionInformation.Status == TransactionStatus.Committed)
                    Start(action, operationName, timeout);
            };
            return;
        }
        Start(action, operationName, timeout);
    }

    private static void Start(Func<CancellationToken, Task> action, string operationName, TimeSpan? timeout) {
        // Fire-and-forget work must not inherit a completed/disposed ambient transaction.
        using var suppressed = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
        _ = Task.Run(async () => {
            using CancellationTokenSource timeoutCts = new(timeout ?? _defaultTimeout);

            try {
                await action(timeoutCts.Token);
            } catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested) {
                _logger.Warn("Background operation '{0}' timed out after {1} seconds", operationName, (timeout ?? _defaultTimeout).TotalSeconds);
            } catch (Exception exc) {
                _logger.Error(exc, "Background operation '{0}' failed", operationName);
            }
        });
    }
}
