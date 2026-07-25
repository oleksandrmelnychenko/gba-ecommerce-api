using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using GBA.Services.Services.Products;
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
    private const int OrphanProbeSampleSize = 50;
    private const double OrphanProbeLiveRatioThreshold = 0.5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SyncSettings _settings;
    private readonly ILogger<ProductSearchSyncBackgroundService> _log;

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
        DateTime nowUtc = DateTime.UtcNow;

        using IServiceScope scope = _scopeFactory.CreateScope();
        IServiceProvider provider = scope.ServiceProvider;

        ISearchSyncStateStore stateStore = provider.GetRequiredService<ISearchSyncStateStore>();
        SearchSyncState state = await stateStore.GetStateAsync(ct);
        bool schemaRebuildDue = state.RequiresFullRebuild(SearchIndexSchema.CurrentVersion);
        bool orphanedGeneration = await IsActiveGenerationOrphanedAsync(provider, stateStore, ct);
        bool fullRebuildDue = schemaRebuildDue || orphanedGeneration || IsFullRebuildDue(nowUtc, state);

        if (orphanedGeneration) {
            _log.LogWarning(
                "Active search generation indexes product ids that no longer exist (1C re-mint); forcing a full rebuild");
        }

        if (schemaRebuildDue) {
            _log.LogInformation(
                "Search pricing schema changed from {StoredSchema} to {RequiredSchema}; rebuilding the alias immediately",
                state.SchemaVersion ?? "<unset>",
                SearchIndexSchema.CurrentVersion);
        }

        // Surface staleness before running: a watermark older than the SLA means the index
        // is lagging behind live stock (the exact failure mode behind the catalog/cart divergence).
        DateTime watermark = state.WatermarkUtc;
        if (watermark != DateTime.MinValue) {
            double lagSeconds = (nowUtc - watermark).TotalSeconds;
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

        if (!result.Success) {
            _log.LogWarning("Product search {Kind} sync failed: {Error}",
                fullRebuildDue ? "full" : "incremental", result.Error);
        }
    }

    /// <summary>
    /// A 1C re-mint replaces every product id, which silently turns the whole generation into
    /// phantom documents. Sample the live generation and force a rebuild when the ids are dead.
    /// </summary>
    private static async Task<bool> IsActiveGenerationOrphanedAsync(
        IServiceProvider provider,
        ISearchSyncStateStore stateStore,
        CancellationToken ct) {
        SearchActiveGeneration? active = await stateStore.GetActiveGenerationAsync(ct);
        if (active == null || string.IsNullOrWhiteSpace(active.IndexName)) return false;

        IReadOnlyList<long> sample = await provider.GetRequiredService<IElasticsearchIndexService>()
            .SampleGenerationProductIdsAsync(active.IndexName, OrphanProbeSampleSize, ct);
        if (sample.Count < OrphanProbeSampleSize) return false;

        int live = await provider.GetRequiredService<IProductSyncRepository>()
            .CountLiveProductIdsAsync(sample);

        return live <= sample.Count * OrphanProbeLiveRatioThreshold;
    }

    /// <summary>
    /// Due-time scheduling, not hour matching: a replica that was down (or busy) during the
    /// configured hour still runs the missed rebuild instead of skipping a whole day.
    /// </summary>
    private bool IsFullRebuildDue(DateTime nowUtc, SearchSyncState state) {
        return nowUtc >= NextFullRebuildDueUtc(nowUtc, state);
    }

    private DateTime NextFullRebuildDueUtc(DateTime nowUtc, SearchSyncState state) {
        int hour = Math.Clamp(_settings.FullRebuildHour, 0, 23);

        if (!state.LastFullRebuildUtc.HasValue) {
            DateTime todayDue = nowUtc.Date.AddHours(hour);
            return nowUtc >= todayDue ? todayDue : todayDue.AddDays(-1);
        }

        DateTime lastUtc = state.LastFullRebuildUtc.Value.ToUniversalTime();
        DateTime dueAfterLast = lastUtc.Date.AddHours(hour);
        return dueAfterLast <= lastUtc ? dueAfterLast.AddDays(1) : dueAfterLast;
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
