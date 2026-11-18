using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GBA.Search.Elasticsearch;

internal sealed class ElasticsearchTimeoutRetry(ElasticsearchSettings settings, ILogger logger) {
    private const int MaximumTimeoutRetries = 10;
    private const int MaximumRetryDelayMilliseconds = 30000;

    public int AttemptLimit => 1 + Math.Clamp(settings.MaxRetries, 0, MaximumTimeoutRetries);

    public async Task<T> ExecuteAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> executeAttempt,
        CancellationToken ct) {
        int attemptLimit = AttemptLimit;
        for (int attempt = 1; ; attempt++) {
            ct.ThrowIfCancellationRequested();
            try {
                return await executeAttempt(ct);
            } catch (TaskCanceledException ex) when (
                !ct.IsCancellationRequested && IsHttpTimeout(ex) && attempt < attemptLimit) {
                TimeSpan delay = GetTimeoutRetryDelay(attempt, settings.RetryBaseDelayMilliseconds);
                logger.LogWarning(
                    ex,
                    "Elasticsearch {Operation} timed out on attempt {Attempt}; retrying in {DelayMs}ms",
                    operation,
                    attempt,
                    delay.TotalMilliseconds);
                await Task.Delay(delay, ct);
            }
        }
    }

    internal static bool IsHttpTimeout(TaskCanceledException exception) =>
        exception.InnerException is TimeoutException;

    internal static TimeSpan GetTimeoutRetryDelay(
        int retryNumber,
        int baseDelayMilliseconds) {
        ArgumentOutOfRangeException.ThrowIfLessThan(retryNumber, 1);

        int boundedBaseDelay = Math.Clamp(
            baseDelayMilliseconds,
            1,
            MaximumRetryDelayMilliseconds);
        int boundedRetryNumber = Math.Min(retryNumber, MaximumTimeoutRetries);
        long multiplier = 1L << (boundedRetryNumber - 1);
        long delayMilliseconds = Math.Min(
            MaximumRetryDelayMilliseconds,
            boundedBaseDelay * multiplier);
        return TimeSpan.FromMilliseconds(delayMilliseconds);
    }
}
