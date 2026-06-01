using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GBA.Common.Search;
using GBA.Search.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GBA.Ecommerce.Background;

/// <summary>
/// Drains the targeted-reindex signal, coalesces bursts within a short debounce window,
/// and re-indexes just the affected products — giving near-real-time freshness on top of
/// the periodic incremental sync.
/// </summary>
public sealed class ProductReindexBackgroundService : BackgroundService {
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(1);
    private const int MaxBatch = 500;

    private readonly ISearchReindexSignal _signal;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductReindexBackgroundService> _log;

    public ProductReindexBackgroundService(
        ISearchReindexSignal signal,
        IServiceScopeFactory scopeFactory,
        ILogger<ProductReindexBackgroundService> logger) {
        _signal = signal;
        _scopeFactory = scopeFactory;
        _log = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        HashSet<long> batch = new();

        _log.LogInformation("Targeted product reindex consumer started (debounce {Debounce})", DebounceWindow);

        try {
            await foreach (long id in _signal.Reader.ReadAllAsync(stoppingToken)) {
                batch.Add(id);
                while (batch.Count < MaxBatch && _signal.Reader.TryRead(out long more)) batch.Add(more);

                // Coalesce a burst of changes into one reindex call.
                try {
                    await Task.Delay(DebounceWindow, stoppingToken);
                } catch (OperationCanceledException) {
                    break;
                }

                while (batch.Count < MaxBatch && _signal.Reader.TryRead(out long more)) batch.Add(more);

                List<long> ids = batch.ToList();
                batch.Clear();

                try {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    IElasticsearchSyncService sync = scope.ServiceProvider.GetRequiredService<IElasticsearchSyncService>();
                    await sync.ReindexProductsAsync(ids, stoppingToken);
                } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                    break;
                } catch (Exception ex) {
                    _log.LogError(ex, "Targeted reindex of {Count} products failed", ids.Count);
                }
            }
        } catch (OperationCanceledException) {
            // shutdown
        }
    }
}
