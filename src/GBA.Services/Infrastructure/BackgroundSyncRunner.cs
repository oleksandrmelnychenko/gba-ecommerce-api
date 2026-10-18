using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace GBA.Services.Infrastructure;

public static class BackgroundSyncRunner {
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(15);

    public static void Run(
        Func<CancellationToken, Task> action,
        string operationName,
        TimeSpan? timeout = null) {
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
