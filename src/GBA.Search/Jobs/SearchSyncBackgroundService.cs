using System;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Configuration;
using GBA.Search.Sync;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Jobs;

public sealed class SearchSyncBackgroundService(
    IProductSyncService syncService,
    IOptions<SyncSettings> settings,
    ILogger<SearchSyncBackgroundService> logger)
    : BackgroundService {
    private readonly SyncSettings _settings = settings.Value;

    private DateTime _lastFullRebuild = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!_settings.Enabled) {
            logger.LogInformation("Search sync is disabled");
            return;
        }

        logger.LogInformation(
            "Search sync service started. Incremental interval: {Interval}s, Full rebuild hour: {Hour}",
            _settings.IncrementalIntervalSeconds,
            _settings.FullRebuildHour);

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        try {
            logger.LogInformation("Performing initial full rebuild");
            SyncResult result = await syncService.FullRebuildAsync(stoppingToken);
            if (result.Success) {
                _lastFullRebuild = DateTime.UtcNow;
                logger.LogInformation(
                    "Initial full rebuild completed: {Count} documents in {Ms}ms",
                    result.DocumentsIndexed, result.ElapsedMs);
            } else {
                logger.LogError("Initial full rebuild failed: {Error}", result.ErrorMessage);
            }
        } catch (Exception ex) {
            logger.LogError(ex, "Initial full rebuild failed with exception");
        }

        while (!stoppingToken.IsCancellationRequested) {
            try {
                await Task.Delay(
                    TimeSpan.FromSeconds(_settings.IncrementalIntervalSeconds),
                    stoppingToken);

                if (ShouldDoFullRebuild()) {
                    logger.LogInformation("Starting scheduled full rebuild");
                    SyncResult result = await syncService.FullRebuildAsync(stoppingToken);
                    if (result.Success) {
                        _lastFullRebuild = DateTime.UtcNow;
                        logger.LogInformation(
                            "Scheduled full rebuild completed: {Count} documents in {Ms}ms",
                            result.DocumentsIndexed, result.ElapsedMs);
                    } else {
                        logger.LogError("Scheduled full rebuild failed: {Error}", result.ErrorMessage);
                    }
                } else {
                    SyncResult result = await syncService.IncrementalSyncAsync(stoppingToken);
                    if (!result.Success) {
                        logger.LogWarning("Incremental sync failed: {Error}", result.ErrorMessage);
                    } else if (result.DocumentsIndexed > 0 || result.DocumentsDeleted > 0) {
                        logger.LogInformation(
                            "Incremental sync: {Indexed} indexed, {Deleted} deleted in {Ms}ms",
                            result.DocumentsIndexed, result.DocumentsDeleted, result.ElapsedMs);
                    }
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception ex) {
                logger.LogError(ex, "Sync iteration failed");
            }
        }

        logger.LogInformation("Search sync service stopped");
    }

    private bool ShouldDoFullRebuild() {
        DateTime now = DateTime.UtcNow;

        if (now.Hour == _settings.FullRebuildHour &&
            _lastFullRebuild.Date < now.Date) {
            return true;
        }

        return false;
    }
}
