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
        HttpResponseMessage response = await _http.GetAsync($"{_stateIndex}/_doc/{DocId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return DateTime.MinValue;

        string responseBody = await response.Content.ReadAsStringAsync(ct);
        ElasticsearchSyncService.EnsureSuccessfulHttpResponse(
            response,
            "sync watermark read",
            responseBody);

        using JsonDocument doc = JsonDocument.Parse(responseBody);
        if (doc.RootElement.TryGetProperty("_source", out JsonElement source)
            && source.TryGetProperty("lastSyncTime", out JsonElement ts)
            && ts.TryGetDateTime(out DateTime watermark)) {
            return DateTime.SpecifyKind(watermark, DateTimeKind.Utc);
        }

        throw new InvalidOperationException(
            "Elasticsearch sync watermark response does not contain a valid lastSyncTime.");
    }

    public async Task SetWatermarkAsync(DateTime watermarkUtc, CancellationToken ct = default) {
        var body = new { lastSyncTime = watermarkUtc.ToUniversalTime() };
        HttpResponseMessage response = await _http.PutAsJsonAsync($"{_stateIndex}/_doc/{DocId}", body, ct);
        string responseBody = await response.Content.ReadAsStringAsync(ct);
        ElasticsearchSyncService.EnsureSuccessfulHttpResponse(
            response,
            "sync watermark write",
            responseBody);
    }
}
