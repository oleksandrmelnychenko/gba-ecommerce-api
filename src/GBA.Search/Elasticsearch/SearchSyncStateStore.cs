using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Elasticsearch;

/// <summary>
/// Persists the incremental-sync watermark (last successful sync time) so it survives
/// process restarts and is shared across the transient sync-service instances. Stored as a
/// single document in a dedicated Elasticsearch index, which also makes the watermark
/// the single source of truth instead of in-memory state.
/// </summary>
public interface ISearchSyncStateStore {
    Task<DateTime> GetWatermarkAsync(CancellationToken ct = default);
    Task SetWatermarkAsync(DateTime watermarkUtc, CancellationToken ct = default);
}

public sealed class SearchSyncStateStore : ISearchSyncStateStore {
    private const string DocId = "watermark";

    private readonly HttpClient _http;
    private readonly string _stateIndex;
    private readonly ILogger<SearchSyncStateStore> _log;

    public SearchSyncStateStore(
        HttpClient httpClient,
        IOptions<ElasticsearchSettings> settings,
        ILogger<SearchSyncStateStore> logger) {
        _http = httpClient;
        _stateIndex = settings.Value.IndexName + "_sync_state";
        _log = logger;
    }

    public async Task<DateTime> GetWatermarkAsync(CancellationToken ct = default) {
        try {
            HttpResponseMessage response = await _http.GetAsync($"{_stateIndex}/_doc/{DocId}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return DateTime.MinValue;
            if (!response.IsSuccessStatusCode) return DateTime.MinValue;

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("_source", out JsonElement source)
                && source.TryGetProperty("lastSyncTime", out JsonElement ts)
                && ts.TryGetDateTime(out DateTime watermark)) {
                return DateTime.SpecifyKind(watermark, DateTimeKind.Utc);
            }
            return DateTime.MinValue;
        } catch (Exception ex) {
            // Treat an unreadable watermark as "unknown" -> caller falls back to full rebuild.
            _log.LogWarning(ex, "Failed to read sync watermark; treating as unset");
            return DateTime.MinValue;
        }
    }

    public async Task SetWatermarkAsync(DateTime watermarkUtc, CancellationToken ct = default) {
        var body = new { lastSyncTime = watermarkUtc.ToUniversalTime() };
        HttpResponseMessage response = await _http.PutAsJsonAsync($"{_stateIndex}/_doc/{DocId}", body, ct);
        if (!response.IsSuccessStatusCode) {
            string error = await response.Content.ReadAsStringAsync(ct);
            _log.LogWarning("Failed to persist sync watermark: {Error}", error);
        }
    }
}
