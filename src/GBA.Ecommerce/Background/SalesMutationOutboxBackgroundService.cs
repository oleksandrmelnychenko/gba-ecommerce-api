using System;
using System.Threading;
using System.Threading.Tasks;
using GBA.Services.Infrastructure.SalesMutations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GBA.Ecommerce.Background;

public sealed class SalesMutationOutboxBackgroundService : BackgroundService {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SalesMutationOutboxOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SalesMutationOutboxBackgroundService> _logger;

    public SalesMutationOutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        SalesMutationOutboxOptions options,
        TimeProvider timeProvider,
        ILogger<SalesMutationOutboxBackgroundService> logger) {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options.Validate();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        DateTime nextCleanupUtc = UtcNow();

        while (!stoppingToken.IsCancellationRequested) {
            try {
                SalesMutationDeliveryResult result;
                using (IServiceScope scope = _scopeFactory.CreateScope()) {
                    ISalesMutationOutboxDispatcher dispatcher =
                        scope.ServiceProvider.GetRequiredService<ISalesMutationOutboxDispatcher>();
                    result = await dispatcher.ProcessNextAsync(stoppingToken);
                }

                LogResult(result);

                if (UtcNow() >= nextCleanupUtc) {
                    await DeleteExpiredCompletedRowsAsync(stoppingToken);
                    nextCleanupUtc = UtcNow().Add(_options.CleanupInterval);
                }

                if (result.Kind == SalesMutationDeliveryKind.None)
                    await Task.Delay(_options.PollInterval, _timeProvider, stoppingToken);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception exception) {
                _logger.LogError(exception, "Durable sales mutation outbox dispatch loop failed");
                await Task.Delay(_options.PollInterval, _timeProvider, stoppingToken);
            }
        }
    }

    private async Task DeleteExpiredCompletedRowsAsync(CancellationToken cancellationToken) {
        // Only transport receipts expire. EcommerceSalesCreationLedger is the permanent replay record.
        using IServiceScope scope = _scopeFactory.CreateScope();
        ISalesMutationOutboxStore store =
            scope.ServiceProvider.GetRequiredService<ISalesMutationOutboxStore>();
        int deleted = await store.DeleteCompletedBeforeAsync(
            UtcNow().Subtract(_options.DispatchCompletedRetention),
            cancellationToken);
        if (deleted > 0)
            _logger.LogInformation("Deleted {DeletedCount} expired sales mutation outbox receipts", deleted);
    }

    private void LogResult(SalesMutationDeliveryResult result) {
        switch (result.Kind) {
            case SalesMutationDeliveryKind.Completed:
                _logger.LogInformation(
                    "Delivered sales mutation {OperationNetUid} on attempt {AttemptCount} with HTTP {StatusCode}",
                    result.OperationNetUid,
                    result.AttemptCount,
                    result.StatusCode);
                break;
            case SalesMutationDeliveryKind.Retrying:
                _logger.LogWarning(
                    "Sales mutation {OperationNetUid} attempt {AttemptCount} will retry at {NextAttemptUtc}; HTTP {StatusCode}",
                    result.OperationNetUid,
                    result.AttemptCount,
                    result.NextAttemptUtc,
                    result.StatusCode);
                break;
            case SalesMutationDeliveryKind.DeadLettered:
                _logger.LogError(
                    "Sales mutation {OperationNetUid} entered dead-letter state on attempt {AttemptCount}; HTTP {StatusCode}",
                    result.OperationNetUid,
                    result.AttemptCount,
                    result.StatusCode);
                break;
            case SalesMutationDeliveryKind.LeaseLost:
                _logger.LogWarning(
                    "Sales mutation {OperationNetUid} lost its delivery lease on attempt {AttemptCount}",
                    result.OperationNetUid,
                    result.AttemptCount);
                break;
        }
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
