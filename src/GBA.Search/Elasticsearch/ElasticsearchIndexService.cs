using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Configuration;
using GBA.Search.Sync;
using GBA.Services.Services.Products;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Elasticsearch;

public enum ElasticsearchHealthStatus {
    Healthy,
    Degraded,
    Unhealthy
}

public sealed record ElasticsearchHealthReport(
    ElasticsearchHealthStatus Status,
    bool ClusterAvailable,
    string? ClusterStatus,
    bool HasActiveGeneration,
    bool PointedIndexExists,
    bool ConfigurationConsistent,
    IReadOnlyList<string> Reasons,
    bool AliasConsistent = true,
    bool PricingRevisionsCurrent = false);

public interface IElasticsearchIndexService {
    Task<bool> CreateIndexAsync(CancellationToken ct = default);
    Task<bool> DeleteIndexAsync(CancellationToken ct = default);
    Task<bool> IndexExistsAsync(CancellationToken ct = default);
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
    Task<ElasticsearchHealthReport> GetHealthAsync(CancellationToken ct = default);
    Task<bool> EnsureConcreteIndexAsync(CancellationToken ct = default);

    /// <summary>
    /// Verifies that the configured namespace is not an alias/concrete index of the
    /// opposite configured mode. This method never mutates or migrates that name.
    /// </summary>
    Task<bool> ValidateConfiguredNameModeAsync(bool useAliasSwap, CancellationToken ct = default);

    /// <summary>Creates a new uniquely-named index (with mappings) and returns its name, for alias-swap rebuilds.</summary>
    Task<string?> CreateVersionedIndexAsync(
        SearchRebuildLease lease,
        CancellationToken ct = default);

    /// <summary>Copies one immutable live generation into a new empty staging generation.</summary>
    Task<bool> CloneGenerationAsync(
        SearchRebuildLease lease,
        string sourceIndex,
        string targetIndex,
        CancellationToken ct = default);

    /// <summary>Refreshes only the staging generation owned by the exact fenced coordinator.</summary>
    Task<bool> RefreshGenerationAsync(
        SearchRebuildLease lease,
        string indexName,
        CancellationToken ct = default);

    /// <summary>
    /// Atomically points the search alias at <paramref name="targetIndex"/> while fencing the
    /// cutover with current rebuild-lease ownership.
    /// </summary>
    Task<bool> SwapAliasAsync(
        SearchRebuildLease lease,
        string targetIndex,
        CancellationToken ct = default);

    /// <summary>
    /// Restores the alias to the active index that preceded this lease after a failed promotion.
    /// The rollback is rejected unless the exact lease still owns the target and the alias still
    /// points only at that target.
    /// </summary>
    Task<bool> RestoreAliasAsync(
        SearchRebuildLease lease,
        string failedTargetIndex,
        CancellationToken ct = default);

    /// <summary>Deletes one failed rebuild index when it is not protected by live search state.</summary>
    Task<bool> DeleteFailedVersionedIndexAsync(
        SearchRebuildLease lease,
        string indexName,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes old versioned indices while always preserving the live alias target and active rebuild index.
    /// </summary>
    Task<int> CleanupOldVersionedIndicesAsync(
        SearchRebuildLease lease,
        int keep,
        CancellationToken ct = default);
}

public sealed class ElasticsearchIndexService : IElasticsearchIndexService {
    private readonly HttpClient _http;
    private readonly ElasticsearchSettings _settings;
    private readonly SyncSettings _syncSettings;
    private readonly ISearchSyncStateStore _state;
    private readonly IProductSyncRepository _repository;
    private readonly ILogger<ElasticsearchIndexService> _log;

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ElasticsearchIndexService(
        HttpClient httpClient,
        IOptions<ElasticsearchSettings> settings,
        IOptions<SyncSettings> syncSettings,
        ISearchSyncStateStore state,
        IProductSyncRepository repository,
        ILogger<ElasticsearchIndexService> logger) {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = (settings ?? throw new ArgumentNullException(nameof(settings))).Value;
        _syncSettings = (syncSettings ?? throw new ArgumentNullException(nameof(syncSettings))).Value;
        SearchSyncStorage.ValidateBaseIndexName(_settings.IndexName);
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default) {
        ElasticsearchHealthReport report = await GetHealthAsync(ct);
        return report.Status == ElasticsearchHealthStatus.Healthy;
    }

    public async Task<ElasticsearchHealthReport> GetHealthAsync(CancellationToken ct = default) {
        List<string> reasons = [];
        bool clusterAvailable = false;
        bool aliasConsistent = !_syncSettings.UseAliasSwap;
        string? clusterStatus = null;
        try {
            using HttpResponseMessage response = await _http.GetAsync("_cluster/health", ct);
            if (response.IsSuccessStatusCode) {
                using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                clusterStatus = document.RootElement.TryGetProperty("status", out JsonElement status)
                                && status.ValueKind == JsonValueKind.String
                    ? status.GetString()
                    : null;
                clusterAvailable = clusterStatus is "green" or "yellow";
            }
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            _log.LogWarning(ex, "Elasticsearch cluster health check failed");
        }

        if (!clusterAvailable) {
            reasons.Add("Elasticsearch cluster is unavailable, red, or returned invalid health data.");
            return new ElasticsearchHealthReport(
                ElasticsearchHealthStatus.Unhealthy,
                clusterAvailable,
                clusterStatus,
                HasActiveGeneration: false,
                PointedIndexExists: false,
                ConfigurationConsistent: false,
                reasons,
                aliasConsistent);
        }

        SearchActiveGeneration? active;
        try {
            active = await _state.GetActiveGenerationAsync(ct);
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            _log.LogWarning(ex, "Active search generation health check failed");
            reasons.Add("Active generation state is unreadable.");
            return new ElasticsearchHealthReport(
                ElasticsearchHealthStatus.Unhealthy,
                clusterAvailable,
                clusterStatus,
                HasActiveGeneration: false,
                PointedIndexExists: false,
                ConfigurationConsistent: false,
                reasons,
                aliasConsistent);
        }

        bool hasActiveGeneration = active is {
            Generation: > 0,
            IndexName.Length: > 0
        } && IsVersionedIndexName(active.IndexName);
        if (!hasActiveGeneration) {
            reasons.Add("No valid active search generation pointer exists.");
            return new ElasticsearchHealthReport(
                ElasticsearchHealthStatus.Unhealthy,
                clusterAvailable,
                clusterStatus,
                HasActiveGeneration: false,
                PointedIndexExists: false,
                ConfigurationConsistent: false,
                reasons,
                aliasConsistent);
        }

        bool pointedIndexExists;
        try {
            using HttpRequestMessage request = new(HttpMethod.Head, active!.IndexName);
            using HttpResponseMessage response = await _http.SendAsync(request, ct);
            pointedIndexExists = response.IsSuccessStatusCode;
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            _log.LogWarning(ex, "Pointed search index health check failed");
            pointedIndexExists = false;
        }

        if (!pointedIndexExists) {
            reasons.Add("The active generation points to a missing or unavailable index.");
            return new ElasticsearchHealthReport(
                ElasticsearchHealthStatus.Unhealthy,
                clusterAvailable,
                clusterStatus,
                HasActiveGeneration: true,
                PointedIndexExists: false,
                ConfigurationConsistent: false,
                reasons,
                aliasConsistent);
        }

        bool configurationConsistent = active!.HasConsistentConfiguration;
        try {
            RetailConfigurationSnapshot current =
                await _repository.GetRetailConfigurationSnapshotAsync();
            configurationConsistent = configurationConsistent
                                      && current.IsValid
                                      && string.Equals(
                                          current.Signature,
                                          active.State.RetailConfigurationSignature,
                                          StringComparison.Ordinal);
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            _log.LogWarning(ex, "Retail configuration health check failed");
            configurationConsistent = false;
        }

        if (!configurationConsistent) {
            reasons.Add("The active generation configuration is stale or cannot be verified.");
        }

        bool pricingRevisionsCurrent = false;
        if (configurationConsistent) {
            try {
                PricingDependencyRevisions currentPricingRevisions =
                    await _repository.GetPricingDependencyRevisionsAsync();
                pricingRevisionsCurrent = currentPricingRevisions.IsValid
                                          && active.HasExactIndexedPricingRevisions(
                                              currentPricingRevisions);
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                _log.LogWarning(ex, "Pricing revision health check failed");
            }

            if (!pricingRevisionsCurrent) {
                reasons.Add(
                    "The active generation does not contain the current exact pricing revision.");
            }
        }

        if (_syncSettings.UseAliasSwap) {
            try {
                (bool aliasRead, HashSet<string> aliasTargets) = await ReadAliasTargetsAsync(ct);
                aliasConsistent = aliasRead
                                  && aliasTargets.Count == 1
                                  && aliasTargets.Contains(active.IndexName);
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                _log.LogWarning(ex, "Search alias consistency health check failed");
                aliasConsistent = false;
            }

            if (!aliasConsistent) {
                reasons.Add("The search alias does not point exclusively to the durable active generation.");
            }
        }

        return new ElasticsearchHealthReport(
            !aliasConsistent
                ? ElasticsearchHealthStatus.Unhealthy
                : configurationConsistent && pricingRevisionsCurrent
                    ? ElasticsearchHealthStatus.Healthy
                    : ElasticsearchHealthStatus.Degraded,
            clusterAvailable,
            clusterStatus,
            HasActiveGeneration: true,
            PointedIndexExists: true,
            configurationConsistent,
            reasons,
            aliasConsistent,
            pricingRevisionsCurrent);
    }

    public async Task<bool> IndexExistsAsync(CancellationToken ct = default) {
        SearchActiveGeneration? active = await _state.GetActiveGenerationAsync(ct);
        if (active == null || !IsVersionedIndexName(active.IndexName)) return false;

        using HttpRequestMessage request = new(HttpMethod.Head, active.IndexName);
        using HttpResponseMessage response = await _http.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    public Task<bool> EnsureConcreteIndexAsync(CancellationToken ct = default) =>
        Task.FromException<bool>(DirectIndexMutationDisabled());

    public async Task<bool> ValidateConfiguredNameModeAsync(
        bool useAliasSwap,
        CancellationToken ct = default) {
        string configuredName = _settings.IndexName;
        using HttpResponseMessage aliasResponse = await _http.GetAsync(
            $"_alias/{configuredName}",
            ct);
        if (aliasResponse.IsSuccessStatusCode) {
            if (!useAliasSwap) {
                _log.LogError(
                    "Search namespace {Name} is an alias but UseAliasSwap is false; refusing unsafe migration",
                    configuredName);
                return false;
            }

            return true;
        }

        if (aliasResponse.StatusCode != HttpStatusCode.NotFound) {
            string aliasError = await aliasResponse.Content.ReadAsStringAsync(ct);
            _log.LogError(
                "Could not determine whether search namespace {Name} is an alias: {Error}",
                configuredName,
                aliasError);
            return false;
        }

        using HttpResponseMessage concreteResponse = await _http.GetAsync(configuredName, ct);
        if (concreteResponse.IsSuccessStatusCode) {
            if (useAliasSwap) {
                _log.LogError(
                    "Search namespace {Name} is a concrete index but UseAliasSwap is true; refusing destructive migration",
                    configuredName);
                return false;
            }

            return true;
        }

        if (concreteResponse.StatusCode == HttpStatusCode.NotFound) return true;

        string concreteError = await concreteResponse.Content.ReadAsStringAsync(ct);
        _log.LogError(
            "Could not determine whether search namespace {Name} is a concrete index: {Error}",
            configuredName,
            concreteError);
        return false;
    }

    public Task<bool> DeleteIndexAsync(CancellationToken ct = default) =>
        Task.FromException<bool>(DirectIndexMutationDisabled());

    public Task<bool> CreateIndexAsync(CancellationToken ct = default) =>
        Task.FromException<bool>(DirectIndexMutationDisabled());

    private async Task<bool> CreateIndexAsync(string indexName, CancellationToken ct) {
        object indexSettings = BuildIndexSettings();
        string json = JsonSerializer.Serialize(indexSettings, JsonOptions);
        StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _http.PutAsync(indexName, content, ct);

        if (response.IsSuccessStatusCode) {
            _log.LogInformation("Created index {Index}", indexName);
            return true;
        }

        string error = await response.Content.ReadAsStringAsync(ct);
        _log.LogError("Failed to create index {Index}: {Error}", indexName, error);
        return false;
    }

    public async Task<string?> CreateVersionedIndexAsync(
        SearchRebuildLease lease,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        if (!await _state.ValidateWriteLeaseAsync(lease, string.Empty, ct)) {
            _log.LogWarning("Refusing index creation for a stale or unbound coordinator lease");
            return null;
        }

        string indexName = $"{_settings.IndexName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}";
        return await CreateIndexAsync(indexName, ct) ? indexName : null;
    }

    public async Task<bool> CloneGenerationAsync(
        SearchRebuildLease lease,
        string sourceIndex,
        string targetIndex,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        if (!IsVersionedIndexName(sourceIndex) || !IsVersionedIndexName(targetIndex)) {
            _log.LogError(
                "Refusing generation clone with invalid source {Source} or target {Target}",
                sourceIndex,
                targetIndex);
            return false;
        }
        if (!await _state.ValidateWriteLeaseAsync(lease, targetIndex, ct)) {
            _log.LogWarning(
                "Refusing generation clone for stale coordinator {OwnerId}/{FencingToken}",
                lease.OwnerId,
                lease.FencingToken);
            return false;
        }

        var body = new {
            source = new { index = sourceIndex },
            dest = new { index = targetIndex, op_type = "create" },
            conflicts = "abort"
        };
        using StringContent content = new(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");
        using HttpResponseMessage response = await _http.PostAsync(
            "_reindex?wait_for_completion=true&refresh=false",
            content,
            ct);
        if (!response.IsSuccessStatusCode) {
            string error = await response.Content.ReadAsStringAsync(ct);
            _log.LogError(
                "Failed to clone search generation {Source} -> {Target}: {Error}",
                sourceIndex,
                targetIndex,
                error);
            return false;
        }

        try {
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(ct));
            JsonElement root = document.RootElement;
            int failureCount = root.TryGetProperty("failures", out JsonElement failures)
                               && failures.ValueKind == JsonValueKind.Array
                ? failures.GetArrayLength()
                : -1;
            long total = root.TryGetProperty("total", out JsonElement totalElement)
                         && totalElement.TryGetInt64(out long parsedTotal)
                ? parsedTotal
                : -1;
            long created = root.TryGetProperty("created", out JsonElement createdElement)
                           && createdElement.TryGetInt64(out long parsedCreated)
                ? parsedCreated
                : -1;
            long updated = root.TryGetProperty("updated", out JsonElement updatedElement)
                           && updatedElement.TryGetInt64(out long parsedUpdated)
                ? parsedUpdated
                : -1;

            if (failureCount != 0 || total < 0 || created < 0 || updated < 0 || created + updated != total) {
                _log.LogError(
                    "Generation clone {Source} -> {Target} was incomplete: total={Total}, created={Created}, updated={Updated}, failures={Failures}",
                    sourceIndex,
                    targetIndex,
                    total,
                    created,
                    updated,
                    failureCount);
                return false;
            }

            return true;
        } catch (JsonException ex) {
            _log.LogError(
                ex,
                "Generation clone {Source} -> {Target} returned invalid JSON",
                sourceIndex,
                targetIndex);
            return false;
        }
    }

    public async Task<bool> RefreshGenerationAsync(
        SearchRebuildLease lease,
        string indexName,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        if (!IsVersionedIndexName(indexName)
            || !await _state.ValidateWriteLeaseAsync(lease, indexName, ct)) {
            _log.LogWarning("Refusing staging refresh for an invalid index or stale coordinator lease");
            return false;
        }

        using HttpResponseMessage response = await _http.PostAsync($"{indexName}/_refresh", null, ct);
        if (response.IsSuccessStatusCode) return true;

        string error = await response.Content.ReadAsStringAsync(ct);
        _log.LogError("Failed to refresh staging generation {Index}: {Error}", indexName, error);
        return false;
    }

    public async Task<bool> SwapAliasAsync(
        SearchRebuildLease lease,
        string targetIndex,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        if (!IsVersionedIndexName(targetIndex)) {
            _log.LogError("Refusing to point the search alias at invalid versioned index {Index}", targetIndex);
            return false;
        }
        if (!await _state.ValidateWriteLeaseAsync(lease, targetIndex, ct)) {
            _log.LogWarning(
                "Refusing alias cutover for stale coordinator {OwnerId}/{FencingToken}",
                lease.OwnerId,
                lease.FencingToken);
            return false;
        }

        string alias = _settings.IndexName;
        (bool stateRead, AliasSwapState state) = await ReadAliasSwapStateAsync(alias, ct);
        if (!stateRead) return false;

        // Exact must-exist removals make a delayed request fail after another owner cuts over.
        List<object> actions = [];
        foreach (string currentTarget in state.AliasTargets) {
            actions.Add(new {
                remove = new {
                    index = currentTarget,
                    alias,
                    must_exist = true
                }
            });
        }

        actions.Add(new { add = new { index = targetIndex, alias } });

        if (!await _state.ValidateWriteLeaseAsync(lease, targetIndex, ct)) {
            _log.LogWarning(
                "Refusing alias cutover after losing coordinator fence {OwnerId}/{FencingToken}",
                lease.OwnerId,
                lease.FencingToken);
            return false;
        }

        var body = new { actions };
        using StringContent content = new(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await _http.PostAsync("_aliases", content, ct);

        if (response.IsSuccessStatusCode) {
            _log.LogInformation("Alias {Alias} now points to {Index}", alias, targetIndex);
            return true;
        }

        string error = await response.Content.ReadAsStringAsync(ct);
        _log.LogError("Failed to swap alias {Alias} -> {Index}: {Error}", alias, targetIndex, error);
        return false;
    }

    public async Task<bool> RestoreAliasAsync(
        SearchRebuildLease lease,
        string failedTargetIndex,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        if (!IsVersionedIndexName(failedTargetIndex)
            || (lease.ExpectedActiveIndex != null
                && !IsVersionedIndexName(lease.ExpectedActiveIndex))) {
            _log.LogCritical(
                "Refusing alias rollback with invalid failed target {FailedTarget} or prior target {PriorTarget}",
                failedTargetIndex,
                lease.ExpectedActiveIndex);
            return false;
        }

        if (!await _state.ValidateWriteLeaseAsync(lease, failedTargetIndex, ct)) {
            _log.LogCritical(
                "Cannot roll back alias after promotion failure because coordinator fence {OwnerId}/{FencingToken} is stale",
                lease.OwnerId,
                lease.FencingToken);
            return false;
        }

        string alias = _settings.IndexName;
        (bool stateRead, AliasSwapState state) = await ReadAliasSwapStateAsync(alias, ct);
        if (!stateRead) return false;

        HashSet<string> priorTargets = lease.ExpectedActiveIndex == null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>([lease.ExpectedActiveIndex], StringComparer.Ordinal);
        if (state.AliasTargets.SetEquals(priorTargets)) {
            return true;
        }

        if (state.AliasTargets.Count != 1
            || !state.AliasTargets.Contains(failedTargetIndex)) {
            _log.LogCritical(
                "Cannot safely roll back alias {Alias}; expected only failed target {FailedTarget}, found {Targets}",
                alias,
                failedTargetIndex,
                string.Join(",", state.AliasTargets.OrderBy(value => value, StringComparer.Ordinal)));
            return false;
        }

        if (!await _state.ValidateWriteLeaseAsync(lease, failedTargetIndex, ct)) {
            _log.LogCritical(
                "Cannot roll back alias {Alias}; coordinator fence was lost immediately before rollback",
                alias);
            return false;
        }

        List<object> actions = [
            new {
                remove = new {
                    index = failedTargetIndex,
                    alias,
                    must_exist = true
                }
            }
        ];
        if (lease.ExpectedActiveIndex != null) {
            actions.Add(new { add = new { index = lease.ExpectedActiveIndex, alias } });
        }

        using StringContent content = new(
            JsonSerializer.Serialize(new { actions }, JsonOptions),
            Encoding.UTF8,
            "application/json");
        using HttpResponseMessage response = await _http.PostAsync("_aliases", content, ct);
        if (!response.IsSuccessStatusCode) {
            string error = await response.Content.ReadAsStringAsync(ct);
            _log.LogCritical(
                "Failed to restore alias {Alias} after unsuccessful promotion: {Error}",
                alias,
                error);
            return false;
        }

        (bool verified, AliasSwapState restored) = await ReadAliasSwapStateAsync(alias, ct);
        bool consistent = verified && restored.AliasTargets.SetEquals(priorTargets);
        if (!consistent) {
            _log.LogCritical(
                "Alias {Alias} rollback response succeeded but verification did not match durable target {PriorTarget}",
                alias,
                lease.ExpectedActiveIndex ?? "<none>");
            return false;
        }

        _log.LogWarning(
            "Restored alias {Alias} to durable generation {PriorTarget} after failed promotion",
            alias,
            lease.ExpectedActiveIndex ?? "<none>");
        return true;
    }

    public async Task<bool> DeleteFailedVersionedIndexAsync(
        SearchRebuildLease lease,
        string indexName,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        if (!IsVersionedIndexName(indexName)) {
            _log.LogWarning("Refusing failed-rebuild cleanup for non-versioned index {Index}", indexName);
            return false;
        }
        if (!await _state.ValidateWriteLeaseAsync(lease, indexName, ct)) {
            _log.LogWarning("Refusing failed-index cleanup for a stale coordinator lease");
            return false;
        }

        (bool aliasRead, HashSet<string> aliasTargets) = await ReadAliasTargetsAsync(ct);
        if (!aliasRead || aliasTargets.Contains(indexName)) {
            if (aliasTargets.Contains(indexName)) {
                _log.LogWarning("Keeping failed rebuild index {Index} because it is a live alias target", indexName);
            }
            return false;
        }
        if (!await _state.ValidateWriteLeaseAsync(lease, indexName, ct)) {
            _log.LogWarning("Aborting failed-index cleanup after losing the coordinator fence");
            return false;
        }

        (bool generationRead, HashSet<string> protectedGenerations) =
            await ReadProtectedGenerationIndicesAsync(ct, removableOwnedStagingIndex: indexName);
        if (!generationRead) return false;
        if (protectedGenerations.Contains(indexName)) {
            _log.LogWarning(
                "Keeping failed rebuild index {Index} because live generation state protects it",
                indexName);
            return false;
        }

        if (!await _state.ValidateWriteLeaseAsync(lease, indexName, ct)) {
            _log.LogWarning("Aborting failed-index deletion after losing the coordinator fence");
            return false;
        }

        using HttpResponseMessage response = await _http.DeleteAsync(indexName, ct);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound) {
            _log.LogInformation("Deleted failed rebuild index {Index}", indexName);
            return true;
        }

        string error = await response.Content.ReadAsStringAsync(ct);
        _log.LogWarning("Failed to delete rebuild index {Index}: {Error}", indexName, error);
        return false;
    }

    public async Task<int> CleanupOldVersionedIndicesAsync(
        SearchRebuildLease lease,
        int keep,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        if (keep is < 1 or > 100) {
            throw new ArgumentOutOfRangeException(nameof(keep), "Retention must be between 1 and 100 indices.");
        }
        if (!await _state.ValidateWriteLeaseAsync(lease, ct: ct)) {
            _log.LogWarning("Refusing old-index cleanup for a stale coordinator lease");
            return 0;
        }

        (bool aliasRead, HashSet<string> aliasTargets) = await ReadAliasTargetsAsync(ct);
        if (!aliasRead) return 0;
        if (!await _state.ValidateWriteLeaseAsync(lease, ct: ct)) {
            _log.LogWarning("Aborting old-index cleanup after losing the coordinator fence");
            return 0;
        }

        (bool generationRead, HashSet<string> protectedGenerations) =
            await ReadProtectedGenerationIndicesAsync(ct);
        if (!generationRead) return 0;
        if (!await _state.ValidateWriteLeaseAsync(lease, ct: ct)) {
            _log.LogWarning("Aborting old-index cleanup after losing the coordinator fence");
            return 0;
        }

        using HttpResponseMessage response = await _http.GetAsync(
            $"_cat/indices/{_settings.IndexName}_*?h=index&format=json",
            ct);
        if (!response.IsSuccessStatusCode) {
            string error = await response.Content.ReadAsStringAsync(ct);
            _log.LogWarning("Skipping old-index cleanup because index discovery failed: {Error}", error);
            return 0;
        }

        List<string> indices;
        try {
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            indices = doc.RootElement.EnumerateArray()
                .Select(e => e.TryGetProperty("index", out JsonElement value) ? value.GetString() : null)
                .Where(name => name != null && IsVersionedIndexName(name))
                .Select(name => name!)
                .ToList();
        } catch (Exception ex) when (ex is JsonException or InvalidOperationException) {
            _log.LogWarning(ex, "Skipping old-index cleanup because index discovery returned invalid JSON");
            return 0;
        }

        HashSet<string> protectedIndices = new(aliasTargets, StringComparer.Ordinal);
        protectedIndices.UnionWith(protectedGenerations);

        int protectedVersionedCount = indices.Count(protectedIndices.Contains);
        int backupCount = Math.Max(0, keep - protectedVersionedCount);
        HashSet<string> retainedBackups = indices
            .Where(index => !protectedIndices.Contains(index))
            .OrderByDescending(index => index, StringComparer.Ordinal)
            .Take(backupCount)
            .ToHashSet(StringComparer.Ordinal);

        int deleted = 0;
        foreach (string old in indices.Where(index =>
                     !protectedIndices.Contains(index) && !retainedBackups.Contains(index))) {
            if (!await _state.ValidateWriteLeaseAsync(lease, ct: ct)) {
                _log.LogWarning(
                    "Stopped old-index cleanup before deleting {Index} because the coordinator fence was lost",
                    old);
                break;
            }

            using HttpResponseMessage del = await _http.DeleteAsync(old, ct);
            if (del.IsSuccessStatusCode) {
                deleted++;
                _log.LogInformation("Deleted old index {Index}", old);
            } else {
                string error = await del.Content.ReadAsStringAsync(ct);
                _log.LogWarning("Failed to delete old index {Index}: {Error}", old, error);
            }
        }
        return deleted;
    }

    private async Task<(bool Success, AliasSwapState State)> ReadAliasSwapStateAsync(
        string alias,
        CancellationToken ct) {
        using HttpResponseMessage aliasResponse = await _http.GetAsync($"_alias/{alias}", ct);
        if (aliasResponse.IsSuccessStatusCode) {
            try {
                using JsonDocument document = JsonDocument.Parse(await aliasResponse.Content.ReadAsStringAsync(ct));
                HashSet<string> targets = document.RootElement.EnumerateObject()
                    .Select(property => property.Name)
                    .ToHashSet(StringComparer.Ordinal);
                if (targets.Count == 0) {
                    _log.LogError("Cannot cut over alias {Alias} because alias discovery returned no targets", alias);
                    return (false, AliasSwapState.Empty);
                }

                return (true, new AliasSwapState(targets));
            } catch (Exception ex) when (ex is JsonException or InvalidOperationException) {
                _log.LogError(ex, "Cannot cut over alias {Alias} because alias discovery returned invalid JSON", alias);
                return (false, AliasSwapState.Empty);
            }
        }

        if (aliasResponse.StatusCode != HttpStatusCode.NotFound) {
            string error = await aliasResponse.Content.ReadAsStringAsync(ct);
            _log.LogError("Cannot cut over alias {Alias} because alias discovery failed: {Error}", alias, error);
            return (false, AliasSwapState.Empty);
        }

        using HttpResponseMessage indexResponse = await _http.GetAsync(alias, ct);
        if (indexResponse.IsSuccessStatusCode) {
            _log.LogError(
                "Refusing automatic alias migration because {Index} is a live concrete index",
                alias);
            return (false, AliasSwapState.Empty);
        }

        if (indexResponse.StatusCode == HttpStatusCode.NotFound) {
            return (true, AliasSwapState.Empty);
        }

        string indexError = await indexResponse.Content.ReadAsStringAsync(ct);
        _log.LogError("Cannot inspect legacy concrete index {Index}: {Error}", alias, indexError);
        return (false, AliasSwapState.Empty);
    }

    private async Task<(bool Success, HashSet<string> Targets)> ReadAliasTargetsAsync(CancellationToken ct) {
        using HttpResponseMessage response = await _http.GetAsync($"_alias/{_settings.IndexName}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) {
            return (true, new HashSet<string>(StringComparer.Ordinal));
        }

        if (!response.IsSuccessStatusCode) {
            string error = await response.Content.ReadAsStringAsync(ct);
            _log.LogWarning("Skipping old-index cleanup because alias discovery failed: {Error}", error);
            return (false, new HashSet<string>(StringComparer.Ordinal));
        }

        try {
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            HashSet<string> targets = document.RootElement.EnumerateObject()
                .Select(property => property.Name)
                .ToHashSet(StringComparer.Ordinal);
            return (true, targets);
        } catch (Exception ex) when (ex is JsonException or InvalidOperationException) {
            _log.LogWarning(ex, "Skipping old-index cleanup because alias discovery returned invalid JSON");
            return (false, new HashSet<string>(StringComparer.Ordinal));
        }
    }

    private async Task<(bool Success, HashSet<string> Indices)> ReadProtectedGenerationIndicesAsync(
        CancellationToken ct,
        string? removableOwnedStagingIndex = null) {
        string stateIndex = SearchSyncStorage.GetStateIndex(_settings.IndexName);
        using HttpResponseMessage response = await _http.GetAsync(
            $"{stateIndex}/_doc/{SearchSyncStorage.GenerationControlDocumentId}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound) {
            return await ReadLegacyActiveRebuildIndexAsync(stateIndex, ct);
        }

        if (!response.IsSuccessStatusCode) {
            string error = await response.Content.ReadAsStringAsync(ct);
            _log.LogWarning(
                "Skipping old-index cleanup because generation-control discovery failed: {Error}",
                error);
            return (false, []);
        }

        try {
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!document.RootElement.TryGetProperty("_source", out JsonElement source)) {
                _log.LogWarning("Skipping old-index cleanup because generation control has no source");
                return (false, []);
            }

            HashSet<string> protectedIndices = new(StringComparer.Ordinal);
            AddOptionalIndex(source, "activeIndex", protectedIndices);

            DateTime? expiration = ReadUtcDateTime(source, "leaseExpiresAtUtc");
            if (expiration > DateTime.UtcNow) {
                string? stagingIndex = ReadOptionalIndex(source, "stagingIndex");
                if (!string.Equals(
                        stagingIndex,
                        removableOwnedStagingIndex,
                        StringComparison.Ordinal)
                    && stagingIndex != null) {
                    protectedIndices.Add(stagingIndex);
                }
            }

            return (true, protectedIndices);
        } catch (Exception ex) when (ex is JsonException or InvalidOperationException) {
            _log.LogWarning(
                ex,
                "Skipping old-index cleanup because generation control returned invalid JSON");
            return (false, []);
        }
    }

    private async Task<(bool Success, HashSet<string> Indices)> ReadLegacyActiveRebuildIndexAsync(
        string stateIndex,
        CancellationToken ct) {
        using HttpResponseMessage response = await _http.GetAsync(
            $"{stateIndex}/_doc/{SearchSyncStorage.RebuildLeaseDocumentId}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return (true, []);
        if (!response.IsSuccessStatusCode) return (false, []);

        try {
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!document.RootElement.TryGetProperty("_source", out JsonElement source)) return (false, []);
            DateTime? expiration = ReadUtcDateTime(source, SearchSyncStorage.LeaseExpirationProperty);
            if (expiration <= DateTime.UtcNow) return (true, []);

            HashSet<string> protectedIndices = new(StringComparer.Ordinal);
            AddOptionalIndex(source, SearchSyncStorage.LeaseIndexProperty, protectedIndices);
            return (true, protectedIndices);
        } catch (Exception ex) when (ex is JsonException or InvalidOperationException) {
            _log.LogWarning(ex, "Legacy rebuild lease returned invalid JSON");
            return (false, []);
        }
    }

    private static void AddOptionalIndex(
        JsonElement source,
        string propertyName,
        ISet<string> indices) {
        if (source.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString())) {
            indices.Add(value.GetString()!);
        }
    }

    private static string? ReadOptionalIndex(JsonElement source, string propertyName) {
        return source.TryGetProperty(propertyName, out JsonElement value)
               && value.ValueKind == JsonValueKind.String
               && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()
            : null;
    }

    private bool IsVersionedIndexName(string indexName) {
        return SearchSyncStorage.IsVersionedIndexName(_settings.IndexName, indexName);
    }

    private static InvalidOperationException DirectIndexMutationDisabled() {
        return new InvalidOperationException(
            "Direct search-index mutation is disabled. Use the fenced generation sync coordinator.");
    }

    private static DateTime? ReadUtcDateTime(JsonElement source, string propertyName) {
        if (!source.TryGetProperty(propertyName, out JsonElement value)
            || !value.TryGetDateTime(out DateTime parsed)) {
            return null;
        }

        return parsed.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : parsed.ToUniversalTime();
    }

    private sealed record AliasSwapState(
        HashSet<string> AliasTargets) {
        internal static AliasSwapState Empty { get; } = new([]);
    }

    private static object BuildIndexSettings() {
        return new {
            settings = new {
                number_of_shards = 1,
                number_of_replicas = 0,
                max_ngram_diff = 15,
                analysis = new {
                    analyzer = new {
                        // Ngram analyzer for substring matching (LIKE '%term%')
                        ngram_analyzer = new {
                            type = "custom",
                            tokenizer = "ngram_tokenizer",
                            filter = new[] { "lowercase" }
                        },
                        // Ukrainian analyzer for morphology
                        ukrainian_analyzer = new {
                            type = "custom",
                            tokenizer = "standard",
                            filter = new[] { "lowercase", "ukrainian_stemmer" }
                        },
                        // Simple lowercase analyzer
                        lowercase_analyzer = new {
                            type = "custom",
                            tokenizer = "keyword",
                            filter = new[] { "lowercase" }
                        }
                    },
                    tokenizer = new {
                        ngram_tokenizer = new {
                            type = "ngram",
                            min_gram = 3,
                            max_gram = 15,
                            token_chars = new[] { "letter", "digit" }
                        }
                    },
                    filter = new {
                        ukrainian_stemmer = new {
                            type = "stemmer",
                            language = "russian" // Elasticsearch doesn't have Ukrainian, Russian is closest
                        }
                    }
                }
            },
            mappings = new {
                properties = new {
                    id = new { type = "long" },
                    netUid = new { type = "keyword" },

                    // Vendor code - exact and substring
                    vendorCode = new { type = "keyword" },
                    vendorCodeClean = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },

                    // Names - full text, morphology, and substring
                    name = new {
                        type = "text",
                        analyzer = "standard",
                        fields = new {
                            keyword = new { type = "keyword" },
                            ukrainian = new {
                                type = "text",
                                analyzer = "ukrainian_analyzer"
                            }
                        }
                    },
                    nameUA = new {
                        type = "text",
                        analyzer = "standard",
                        fields = new {
                            keyword = new { type = "keyword" },
                            ukrainian = new {
                                type = "text",
                                analyzer = "ukrainian_analyzer"
                            }
                        }
                    },

                    // Search names (no spaces) - for PATINDEX-like matching
                    searchName = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },
                    searchNameUA = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },

                    // Descriptions
                    description = new {
                        type = "text",
                        analyzer = "standard",
                        fields = new {
                            ukrainian = new {
                                type = "text",
                                analyzer = "ukrainian_analyzer"
                            }
                        }
                    },
                    descriptionUA = new {
                        type = "text",
                        analyzer = "standard",
                        fields = new {
                            ukrainian = new {
                                type = "text",
                                analyzer = "ukrainian_analyzer"
                            }
                        }
                    },
                    searchDescription = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },
                    searchDescriptionUA = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },

                    // Original numbers - exact and substring
                    mainOriginalNumber = new { type = "keyword" },
                    mainOriginalNumberClean = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },
                    originalNumbers = new { type = "keyword" },
                    originalNumbersClean = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },

                    // Size
                    size = new { type = "keyword" },
                    sizeClean = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },

                    // Product details
                    packingStandard = new { type = "keyword" },
                    orderStandard = new { type = "keyword" },
                    ucgfea = new { type = "keyword" },
                    volume = new { type = "keyword" },
                    top = new { type = "keyword" },
                    weight = new { type = "float" },
                    hasAnalogue = new { type = "boolean" },
                    hasComponent = new { type = "boolean" },
                    hasImage = new { type = "boolean" },
                    image = new { type = "keyword" },
                    measureUnitId = new { type = "long" },

                    // Availability
                    available = new { type = "boolean" },
                    availableQtyUk = new { type = "float" },
                    availableQtyUkVat = new { type = "float" },
                    availableQtyPl = new { type = "float" },
                    availableQtyPlVat = new { type = "float" },
                    availableQty = new { type = "float" },

                    // Flags
                    isForWeb = new { type = "boolean" },
                    isForSale = new { type = "boolean" },
                    isForZeroSale = new { type = "boolean" },

                    // Slug
                    slugId = new { type = "long" },
                    slugNetUid = new { type = "keyword" },
                    slugUrl = new { type = "keyword" },
                    slugLocale = new { type = "keyword" },

                    // Retail price and exact catalog identity. Source/agreement strings keep
                    // a keyword subfield so upgraded dynamic mappings and rebuilt indices
                    // use the same query path during rollout.
                    retailPrice = new { type = "scaled_float", scaling_factor = 100 },
                    retailPriceVat = new { type = "scaled_float", scaling_factor = 100 },
                    retailCurrencyCode = new { type = "keyword" },
                    retailCurrencyCodeVat = new { type = "keyword" },
                    indexedProductPricingRevision = new { type = "keyword" },
                    indexedPricingHierarchyRevision = new { type = "keyword" },
                    indexedDiscountRevision = new { type = "keyword" },
                    indexedExchangeRateRevision = new { type = "keyword" },
                    catalogOrganizationIdNonVat = new { type = "long" },
                    catalogOrganizationIdVat = new { type = "long" },
                    catalogAgreementSourceNonVat = new {
                        type = "text",
                        fields = new { keyword = new { type = "keyword" } }
                    },
                    catalogAgreementSourceVat = new {
                        type = "text",
                        fields = new { keyword = new { type = "keyword" } }
                    },
                    productSourceFenix = new { type = "keyword" },
                    productSourceAmg = new { type = "keyword" },
                    isCanonicalFenix = new { type = "boolean" },
                    isCanonicalAmg = new { type = "boolean" },
                    catalogScopes = new {
                        type = "nested",
                        properties = new {
                            organizationId = new { type = "long" },
                            sourceSystem = new { type = "keyword" },
                            withVat = new { type = "boolean" },
                            availableQtyUk = new { type = "float" },
                            availableQtyPl = new { type = "float" },
                            availableQty = new { type = "float" }
                        }
                    },
                    catalogSourceSystemNonVat = new { type = "keyword" },
                    catalogSourceSystemVat = new { type = "keyword" },
                    catalogAgreementNetUidNonVat = new {
                        type = "text",
                        fields = new { keyword = new { type = "keyword" } }
                    },
                    catalogAgreementNetUidVat = new {
                        type = "text",
                        fields = new { keyword = new { type = "keyword" } }
                    },
                    catalogPricingIdNonVat = new { type = "long" },
                    catalogPricingIdVat = new { type = "long" },
                    catalogCurrencyIdNonVat = new { type = "long" },
                    catalogCurrencyIdVat = new { type = "long" },
                    hasNonVatCatalogAvailability = new { type = "boolean" },
                    hasVatCatalogAvailability = new { type = "boolean" },
                    hasNonVatCatalogSource = new { type = "boolean" },
                    hasVatCatalogSource = new { type = "boolean" },

                    updatedAt = new { type = "date" }
                }
            }
        };
    }
}
