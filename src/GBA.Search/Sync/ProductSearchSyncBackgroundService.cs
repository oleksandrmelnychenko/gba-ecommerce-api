using System;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Sync;

/// <summary>
/// Drives the Elasticsearch product index on a schedule: an incremental sync every
/// <see cref="SyncSettings.IncrementalIntervalSeconds"/> and a full rebuild once a day at
/// <see cref="SyncSettings.FullRebuildHour"/>. On first run (no watermark) the incremental
/// sync self-heals into a full rebuild. The sync service is resolved per-iteration from a
/// scope because this hosted service is a singleton while the sync service is transient.
/// </summary>
public sealed class ProductSearchSyncBackgroundService : BackgroundService {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SyncSettings _settings;
    private readonly ILogger<ProductSearchSyncBackgroundService> _log;

    private DateOnly? _lastFullRebuildDate;

    public ProductSearchSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<SyncSettings> settings,
        ILogger<ProductSearchSyncBackgroundService> logger) {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _log = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!_settings.Enabled) {
            _log.LogInformation("Product search sync disabled (SearchSync:Enabled=false); background service idle");
            return;
        }

        // Give the host time to finish starting before the first (possibly heavy) sync.
        if (!await DelayAsync(TimeSpan.FromSeconds(10), stoppingToken)) return;

        TimeSpan interval = TimeSpan.FromSeconds(Math.Max(5, _settings.IncrementalIntervalSeconds));
        _log.LogInformation(
            "Product search sync started: incremental every {Interval}s, full rebuild at {Hour}:00 UTC",
            interval.TotalSeconds, _settings.FullRebuildHour);

        while (!stoppingToken.IsCancellationRequested) {
            try {
                await RunOnceAsync(stoppingToken);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception ex) {
                // Never let a failed iteration kill the loop.
                _log.LogError(ex, "Product search sync iteration threw; retrying next interval");
            }

            if (!await DelayAsync(interval, stoppingToken)) break;
        }
    }

    private async Task RunOnceAsync(CancellationToken ct) {
        bool fullRebuildDue = IsFullRebuildDue(DateTime.UtcNow);

        using IServiceScope scope = _scopeFactory.CreateScope();
        IServiceProvider provider = scope.ServiceProvider;

        // Surface staleness before running: a watermark older than the SLA means the index
        // is lagging behind live stock (the exact failure mode behind the catalog/cart divergence).
        DateTime watermark = await provider.GetRequiredService<ISearchSyncStateStore>().GetWatermarkAsync(ct);
        if (watermark != DateTime.MinValue) {
            double lagSeconds = (DateTime.UtcNow - watermark).TotalSeconds;
            if (lagSeconds > _settings.LagWarningSeconds) {
                _log.LogWarning(
                    "Search index stale: {LagSeconds:0}s since last successful sync (watermark {Watermark:o}, SLA {Sla}s)",
                    lagSeconds, watermark, _settings.LagWarningSeconds);
            }
        }

        IElasticsearchSyncService sync = provider.GetRequiredService<IElasticsearchSyncService>();

        SyncResult result = fullRebuildDue
            ? await sync.FullRebuildAsync(ct)
            : await sync.IncrementalSyncAsync(ct);

        if (result.Success) {
            if (fullRebuildDue) _lastFullRebuildDate = DateOnly.FromDateTime(DateTime.UtcNow);
        } else {
            _log.LogWarning("Product search {Kind} sync failed: {Error}",
                fullRebuildDue ? "full" : "incremental", result.Error);
        }
    }

    private bool IsFullRebuildDue(DateTime nowUtc) {
        DateOnly today = DateOnly.FromDateTime(nowUtc);
        return nowUtc.Hour == _settings.FullRebuildHour && _lastFullRebuildDate != today;
    }

    private static async Task<bool> DelayAsync(TimeSpan delay, CancellationToken ct) {
        try {
            await Task.Delay(delay, ct);
            return true;
        } catch (OperationCanceledException) {
            return false;
        }
    }
}
