using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GBA.Services.Services.Products;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Elasticsearch;

/// <summary>
/// Stores the shared search-sync checkpoint and coordinates full rebuild ownership across replicas.
/// </summary>
public interface ISearchSyncStateStore {
    /// <summary>Returns the authoritative generation used by public search.</summary>
    Task<SearchActiveGeneration?> GetActiveGenerationAsync(CancellationToken ct = default);

    /// <summary>Acquires the one distributed write lease shared by every sync mode.</summary>
    Task<SearchRebuildLease?> TryAcquireWriteLeaseAsync(
        TimeSpan leaseDuration,
        CancellationToken ct = default);

    /// <summary>
    /// Binds the SQL configuration observed by the coordinator to its durable lease fence.
    /// A changed signature advances the shared configuration epoch under Elasticsearch CAS.
    /// </summary>
    Task<SearchRebuildLease?> BindWriteLeaseConfigurationAsync(
        SearchRebuildLease lease,
        string configurationSignature,
        TimeSpan leaseDuration,
        CancellationToken ct = default);

    /// <summary>Validates exact live owner, fencing token, generation, configuration epoch, and staging index.</summary>
    Task<bool> ValidateWriteLeaseAsync(
        SearchRebuildLease lease,
        string? expectedStagingIndex = null,
        CancellationToken ct = default);

    /// <summary>Renews exact owner/fencing ownership and records its staging index.</summary>
    Task<bool> RenewWriteLeaseAsync(
        SearchRebuildLease lease,
        string stagingIndex,
        TimeSpan leaseDuration,
        CancellationToken ct = default);

    /// <summary>
    /// Atomically promotes a validated generation and acknowledges its complete sync/config state.
    /// A stale owner, token, active generation, staging index, or configuration is rejected.
    /// </summary>
    Task<SearchGenerationAcknowledgement?> PromoteGenerationAsync(
        SearchRebuildLease lease,
        string stagingIndex,
        DateTime watermarkUtc,
        string schemaVersion,
        DateTime? fullRebuildUtc,
        string? expectedConfigurationSignature,
        string configurationSignature,
        PricingDependencyRevisions indexedPricingRevisions,
        CancellationToken ct = default);

    /// <summary>Releases exact owner/fencing ownership; otherwise the lease expires naturally.</summary>
    Task ReleaseWriteLeaseAsync(SearchRebuildLease lease, CancellationToken ct = default);

    /// <summary>Gets the latest durable search-sync state.</summary>
    Task<SearchSyncState> GetStateAsync(CancellationToken ct = default);

    /// <summary>Gets the latest durable incremental-sync watermark.</summary>
    Task<DateTime> GetWatermarkAsync(CancellationToken ct = default);

    /// <summary>Advances the incremental-sync state without changing the last full-rebuild time.</summary>
    Task<bool> SetStateAsync(
        DateTime watermarkUtc,
        string schemaVersion,
        string? expectedConfigurationSignature,
        string configurationSignature,
        CancellationToken ct = default);

    /// <summary>Records a successfully cut-over full rebuild.</summary>
    Task<bool> SetFullRebuildStateAsync(
        DateTime watermarkUtc,
        string schemaVersion,
        DateTime fullRebuildUtc,
        string? expectedConfigurationSignature,
        string configurationSignature,
        CancellationToken ct = default);

    /// <summary>Attempts to acquire the cross-replica full-rebuild lease.</summary>
    Task<SearchRebuildLease?> TryAcquireRebuildLeaseAsync(TimeSpan leaseDuration, CancellationToken ct = default);

    /// <summary>Renews a lease owned by this rebuild and optionally records its in-progress index.</summary>
    Task<bool> RenewRebuildLeaseAsync(
        SearchRebuildLease lease,
        string? ownedIndex,
        TimeSpan leaseDuration,
        CancellationToken ct = default);

    /// <summary>
    /// Atomically makes the current lease non-expiring before alias mutation. This favors
    /// safety over liveness: a process crash requires explicit recovery instead of allowing
    /// a stale owner to cut over after another owner takes the lease.
    /// </summary>
    Task<bool> BeginAliasCutoverAsync(
        SearchRebuildLease lease,
        string ownedIndex,
        CancellationToken ct = default);

    /// <summary>Releases the lease only when it is still owned by this rebuild.</summary>
    Task ReleaseRebuildLeaseAsync(SearchRebuildLease lease, CancellationToken ct = default);
}

/// <summary>Durable state shared by scheduled sync workers.</summary>
public sealed record SearchSyncState(
    DateTime WatermarkUtc,
    string? SchemaVersion,
    DateTime? LastFullRebuildUtc = null,
    string? RetailConfigurationSignature = null,
    long RetailConfigurationEpoch = 0,
    PricingDependencyRevisions? IndexedPricingRevisions = null,
    DateTime? LastFullRebuildStartedUtc = null,
    DateTime? LastIncrementalCatchUpUtc = null) {
    /// <summary>Represents a missing or unreadable state document.</summary>
    public static SearchSyncState Empty { get; } = new(DateTime.MinValue, null);

    /// <summary>Returns whether the live alias needs a full rebuild for the required schema.</summary>
    public bool RequiresFullRebuild(string requiredSchemaVersion) {
        return WatermarkUtc == DateTime.MinValue
               || !string.Equals(SchemaVersion, requiredSchemaVersion, StringComparison.Ordinal)
               || !LastFullRebuildUtc.HasValue
               || !LastFullRebuildStartedUtc.HasValue;
    }

    /// <summary>
    /// Returns whether an incremental generation completed after the latest full rebuild.
    /// Missing legacy proof fails closed and forces a new full rebuild followed by catch-up.
    /// </summary>
    public bool HasCompletedRequiredIncrementalCatchUp =>
        LastFullRebuildUtc.HasValue
        && LastFullRebuildStartedUtc.HasValue
        && LastIncrementalCatchUpUtc.HasValue
        && LastIncrementalCatchUpUtc.Value >= LastFullRebuildUtc.Value
        && LastIncrementalCatchUpUtc.Value >= LastFullRebuildStartedUtc.Value;

    /// <summary>Returns whether a successful full rebuild has already been recorded for a UTC date.</summary>
    public bool WasFullyRebuiltOn(DateOnly utcDate) {
        return LastFullRebuildUtc.HasValue
               && DateOnly.FromDateTime(LastFullRebuildUtc.Value) == utcDate;
    }
}

/// <summary>Identifies one full-rebuild lease owner.</summary>
public sealed record SearchRebuildLease(
    string OwnerId,
    long FencingToken = 0,
    string? ExpectedActiveIndex = null,
    long ExpectedGeneration = 0,
    string? ConfigurationSignature = null,
    long ConfigurationEpoch = 0) {
    /// <summary>Returns whether this lease is bound to one durable configuration version.</summary>
    public bool HasConfigurationFence => !string.IsNullOrWhiteSpace(ConfigurationSignature)
                                         && ConfigurationEpoch > 0;
}

/// <summary>The immutable search generation currently visible to requests.</summary>
public sealed record SearchActiveGeneration(
    string IndexName,
    long Generation,
    SearchSyncState State,
    string? ObservedConfigurationSignature = null,
    long ObservedConfigurationEpoch = 0) {
    /// <summary>Returns whether the active generation matches the latest durably observed configuration.</summary>
    public bool HasConsistentConfiguration => State.RetailConfigurationEpoch > 0
                                              && State.RetailConfigurationEpoch
                                              == ObservedConfigurationEpoch
                                              && string.Equals(
                                                  State.RetailConfigurationSignature,
                                                  ObservedConfigurationSignature,
                                                  StringComparison.Ordinal);

    public bool HasExactIndexedPricingRevisions(PricingDependencyRevisions? revisions) {
        return revisions?.MatchesExactly(State.IndexedPricingRevisions) == true;
    }
}

/// <summary>Proof that one exact fenced owner promoted one exact generation.</summary>
public sealed record SearchGenerationAcknowledgement(
    string OwnerId,
    long FencingToken,
    string IndexName,
    long Generation);

internal static class SearchSyncStorage {
    internal const string StateDocumentId = "watermark";
    internal const string RebuildLeaseDocumentId = "rebuild-lock";
    internal const string GenerationControlDocumentId = "generation-control";
    internal const string LeaseOwnerProperty = "ownerId";
    internal const string LeaseExpirationProperty = "leaseExpiresAtUtc";
    internal const string LeaseIndexProperty = "ownedIndex";
    internal const string LeasePhaseProperty = "phase";
    internal const string LeasePhaseBuild = "build";
    internal const string LeasePhaseCutover = "cutover";

    internal static string GetStateIndex(string indexName) => indexName + "_sync_state";

    internal static bool IsVersionedIndexName(string baseIndexName, string indexName) {
        string prefix = baseIndexName + "_";
        if (!indexName.StartsWith(prefix, StringComparison.Ordinal)) return false;

        ReadOnlySpan<char> suffix = indexName.AsSpan(prefix.Length);
        if (suffix.Length == 17 && suffix.IndexOfAnyExceptInRange('0', '9') < 0) return true;
        if (suffix.Length != 50 || suffix[17] != '_') return false;

        ReadOnlySpan<char> timestamp = suffix[..17];
        ReadOnlySpan<char> identifier = suffix[18..];
        return timestamp.IndexOfAnyExceptInRange('0', '9') < 0
               && identifier.Length == 32
               && identifier.IndexOfAnyExcept("0123456789abcdefABCDEF") < 0;
    }

    internal static void ValidateBaseIndexName(string indexName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
        if (indexName.Length > 128
            || !IsLowercaseLetterOrDigit(indexName[0])
            || indexName.Any(character => !IsLowercaseLetterOrDigit(character)
                                          && character is not ('-' or '_'))) {
            throw new ArgumentException(
                "Elasticsearch index name must be 1-128 lowercase ASCII letters, digits, hyphens, or underscores.",
                nameof(indexName));
        }
    }

    private static bool IsLowercaseLetterOrDigit(char value) {
        return value is >= 'a' and <= 'z' or >= '0' and <= '9';
    }
}

/// <summary>
/// Persists sync state and an expiring rebuild lease in Elasticsearch using optimistic concurrency.
/// </summary>
public sealed class SearchSyncStateStore : ISearchSyncStateStore {
    private const int MaxConcurrencyAttempts = 5;

    private readonly HttpClient _http;
    private readonly string _stateIndex;
    private readonly ILogger<SearchSyncStateStore> _log;

    /// <summary>Creates an Elasticsearch-backed sync state store.</summary>
    public SearchSyncStateStore(
        HttpClient httpClient,
        IOptions<ElasticsearchSettings> settings,
        ILogger<SearchSyncStateStore> logger) {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        string indexName = (settings ?? throw new ArgumentNullException(nameof(settings))).Value.IndexName;
        SearchSyncStorage.ValidateBaseIndexName(indexName);
        _stateIndex = SearchSyncStorage.GetStateIndex(indexName);
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SearchSyncState> GetStateAsync(CancellationToken ct = default) {
        try {
            StoredGenerationControl? control = await ReadGenerationControlAsync(ct);
            if (control != null) return control.State;

            StoredState? stored = await ReadStateAsync(ct);
            return stored?.State ?? SearchSyncState.Empty;
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            _log.LogWarning(ex, "Failed to read sync state; treating it as unset");
            return SearchSyncState.Empty;
        }
    }

    public async Task<SearchActiveGeneration?> GetActiveGenerationAsync(
        CancellationToken ct = default) {
        StoredGenerationControl? control = await ReadGenerationControlAsync(ct);
        if (control == null
            || control.Generation <= 0
            || string.IsNullOrWhiteSpace(control.ActiveIndex)) {
            return null;
        }

        return new SearchActiveGeneration(
            control.ActiveIndex,
            control.Generation,
            control.State,
            control.ObservedConfigurationSignature,
            control.ConfigurationEpoch);
    }

    public async Task<SearchRebuildLease?> TryAcquireWriteLeaseAsync(
        TimeSpan leaseDuration,
        CancellationToken ct = default) {
        ValidateLeaseDuration(leaseDuration);
        string ownerId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

        for (int attempt = 0; attempt < MaxConcurrencyAttempts; attempt++) {
            StoredGenerationControl? current = await ReadGenerationControlAsync(ct);
            DateTime now = DateTime.UtcNow;
            if (current != null
                && !string.IsNullOrWhiteSpace(current.LeaseOwnerId)
                && current.LeaseExpiresAtUtc > now) {
                return null;
            }

            long fencingToken = checked((current?.FencingToken ?? 0) + 1);
            SearchRebuildLease lease = new(
                ownerId,
                fencingToken,
                current?.ActiveIndex,
                current?.Generation ?? 0);
            object body = CreateGenerationControlBody(
                current?.State ?? SearchSyncState.Empty,
                current?.ActiveIndex,
                current?.Generation ?? 0,
                ownerId,
                fencingToken,
                now.Add(leaseDuration),
                null,
                current?.ObservedConfigurationSignature,
                current?.ConfigurationEpoch ?? 0);
            string uri = current == null
                ? $"{_stateIndex}/_doc/{SearchSyncStorage.GenerationControlDocumentId}?op_type=create&refresh=wait_for"
                : BuildConditionalDocumentUri(
                    SearchSyncStorage.GenerationControlDocumentId,
                    current.SequenceNumber,
                    current.PrimaryTerm);

            using HttpResponseMessage response = await _http.PutAsJsonAsync(uri, body, ct);
            if (response.IsSuccessStatusCode) return lease;
            if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound) continue;
            throw await CreateStoreExceptionAsync("acquire search write lease", response, ct);
        }

        _log.LogWarning(
            "Could not acquire search write lease after {Attempts} ownership races",
            MaxConcurrencyAttempts);
        return null;
    }

    public async Task<SearchRebuildLease?> BindWriteLeaseConfigurationAsync(
        SearchRebuildLease lease,
        string configurationSignature,
        TimeSpan leaseDuration,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        ValidateConfigurationSignature(configurationSignature);
        ValidateLeaseDuration(leaseDuration);

        for (int attempt = 0; attempt < MaxConcurrencyAttempts; attempt++) {
            StoredGenerationControl? current = await ReadGenerationControlAsync(ct);
            if (!OwnsCurrentLease(current, lease)
                || !MatchesExpectedGeneration(current!, lease)
                || !string.IsNullOrWhiteSpace(current!.StagingIndex)) {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(current.LeaseConfigurationSignature)
                && !string.Equals(
                    current.LeaseConfigurationSignature,
                    configurationSignature,
                    StringComparison.Ordinal)) {
                return null;
            }

            long configurationEpoch = string.Equals(
                    current.ObservedConfigurationSignature,
                    configurationSignature,
                    StringComparison.Ordinal)
                ? Math.Max(1, current.ConfigurationEpoch)
                : checked(current.ConfigurationEpoch + 1);
            if (current.LeaseConfigurationEpoch > 0
                && current.LeaseConfigurationEpoch != configurationEpoch) {
                return null;
            }

            object body = CreateGenerationControlBody(
                current.State,
                current.ActiveIndex,
                current.Generation,
                lease.OwnerId,
                lease.FencingToken,
                DateTime.UtcNow.Add(leaseDuration),
                null,
                configurationSignature,
                configurationEpoch,
                configurationSignature,
                configurationEpoch);
            using HttpResponseMessage response = await _http.PutAsJsonAsync(
                BuildConditionalDocumentUri(
                    SearchSyncStorage.GenerationControlDocumentId,
                    current.SequenceNumber,
                    current.PrimaryTerm),
                body,
                ct);
            if (response.IsSuccessStatusCode) {
                return lease with {
                    ConfigurationSignature = configurationSignature,
                    ConfigurationEpoch = configurationEpoch
                };
            }
            if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound) continue;
            throw await CreateStoreExceptionAsync("bind search configuration fence", response, ct);
        }

        return null;
    }

    public async Task<bool> ValidateWriteLeaseAsync(
        SearchRebuildLease lease,
        string? expectedStagingIndex = null,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        if (!lease.HasConfigurationFence) return false;

        StoredGenerationControl? current = await ReadGenerationControlAsync(ct);
        if (current == null || !OwnsCurrentLease(current, lease)) return false;

        return MatchesLeaseGeneration(current, lease)
               && MatchesConfigurationFence(current, lease)
               && (expectedStagingIndex == null
                   || (expectedStagingIndex.Length == 0
                       && string.IsNullOrWhiteSpace(current.StagingIndex))
                   || string.Equals(
                       current.StagingIndex,
                       expectedStagingIndex,
                       StringComparison.Ordinal));
    }

    public async Task<bool> RenewWriteLeaseAsync(
        SearchRebuildLease lease,
        string stagingIndex,
        TimeSpan leaseDuration,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingIndex);
        ValidateLeaseDuration(leaseDuration);

        for (int attempt = 0; attempt < MaxConcurrencyAttempts; attempt++) {
            StoredGenerationControl? current = await ReadGenerationControlAsync(ct);
            if (!OwnsCurrentLease(current, lease)
                || !MatchesLeaseGeneration(current!, lease)
                || !MatchesConfigurationFence(current!, lease)
                || (!string.IsNullOrWhiteSpace(current!.StagingIndex)
                    && !string.Equals(current.StagingIndex, stagingIndex, StringComparison.Ordinal))) {
                return false;
            }

            object body = CreateGenerationControlBody(
                current!.State,
                current.ActiveIndex,
                current.Generation,
                lease.OwnerId,
                lease.FencingToken,
                DateTime.UtcNow.Add(leaseDuration),
                stagingIndex,
                current.ObservedConfigurationSignature,
                current.ConfigurationEpoch,
                current.LeaseConfigurationSignature,
                current.LeaseConfigurationEpoch);
            using HttpResponseMessage response = await _http.PutAsJsonAsync(
                BuildConditionalDocumentUri(
                    SearchSyncStorage.GenerationControlDocumentId,
                    current.SequenceNumber,
                    current.PrimaryTerm),
                body,
                ct);
            if (response.IsSuccessStatusCode) return true;
            if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound) continue;
            throw await CreateStoreExceptionAsync("renew search write lease", response, ct);
        }

        return false;
    }

    public async Task<SearchGenerationAcknowledgement?> PromoteGenerationAsync(
        SearchRebuildLease lease,
        string stagingIndex,
        DateTime watermarkUtc,
        string schemaVersion,
        DateTime? fullRebuildUtc,
        string? expectedConfigurationSignature,
        string configurationSignature,
        PricingDependencyRevisions indexedPricingRevisions,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingIndex);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSignature);
        ArgumentNullException.ThrowIfNull(indexedPricingRevisions);
        if (!indexedPricingRevisions.IsValid) {
            throw new ArgumentException(
                "Indexed pricing Change Tracking revisions must be complete.",
                nameof(indexedPricingRevisions));
        }

        for (int attempt = 0; attempt < MaxConcurrencyAttempts; attempt++) {
            StoredGenerationControl? current = await ReadGenerationControlAsync(ct);
            if (!OwnsCurrentLease(current, lease)
                || !MatchesExpectedGeneration(current!, lease)
                || !MatchesConfigurationFence(current!, lease)
                || !string.Equals(current!.StagingIndex, stagingIndex, StringComparison.Ordinal)
                || !string.Equals(
                    configurationSignature,
                    lease.ConfigurationSignature,
                    StringComparison.Ordinal)
                || !string.Equals(
                    current.State.RetailConfigurationSignature,
                    expectedConfigurationSignature,
                    StringComparison.Ordinal)) {
                return null;
            }

            long nextGeneration = checked(current.Generation + 1);
            DateTime? nextFullRebuild = MaxUtc(current.State.LastFullRebuildUtc, fullRebuildUtc);
            DateTime? nextFullRebuildStarted = fullRebuildUtc.HasValue
                ? watermarkUtc.ToUniversalTime()
                : current.State.LastFullRebuildStartedUtc;
            DateTime? nextIncrementalCatchUp = fullRebuildUtc.HasValue
                ? null
                : watermarkUtc > current.State.WatermarkUtc
                    ? MaxUtc(current.State.LastIncrementalCatchUpUtc, watermarkUtc)
                    : current.State.LastIncrementalCatchUpUtc;
            SearchSyncState nextState = new(
                watermarkUtc.ToUniversalTime(),
                schemaVersion,
                nextFullRebuild,
                configurationSignature,
                lease.ConfigurationEpoch,
                indexedPricingRevisions,
                nextFullRebuildStarted,
                nextIncrementalCatchUp);
            object body = CreateGenerationControlBody(
                nextState,
                stagingIndex,
                nextGeneration,
                lease.OwnerId,
                lease.FencingToken,
                current.LeaseExpiresAtUtc,
                stagingIndex,
                current.ObservedConfigurationSignature,
                current.ConfigurationEpoch,
                current.LeaseConfigurationSignature,
                current.LeaseConfigurationEpoch);

            using HttpResponseMessage response = await _http.PutAsJsonAsync(
                BuildConditionalDocumentUri(
                    SearchSyncStorage.GenerationControlDocumentId,
                    current.SequenceNumber,
                    current.PrimaryTerm),
                body,
                ct);
            if (response.IsSuccessStatusCode) {
                return new SearchGenerationAcknowledgement(
                    lease.OwnerId,
                    lease.FencingToken,
                    stagingIndex,
                    nextGeneration);
            }
            if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound) continue;
            throw await CreateStoreExceptionAsync("promote search generation", response, ct);
        }

        return null;
    }

    public async Task ReleaseWriteLeaseAsync(
        SearchRebuildLease lease,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);

        try {
            for (int attempt = 0; attempt < MaxConcurrencyAttempts; attempt++) {
                StoredGenerationControl? current = await ReadGenerationControlAsync(ct);
                if (!OwnsCurrentLease(current, lease)) return;

                object body = CreateGenerationControlBody(
                    current!.State,
                    current.ActiveIndex,
                    current.Generation,
                    null,
                    current.FencingToken,
                    null,
                    null,
                    current.ObservedConfigurationSignature,
                    current.ConfigurationEpoch);
                using HttpResponseMessage response = await _http.PutAsJsonAsync(
                    BuildConditionalDocumentUri(
                        SearchSyncStorage.GenerationControlDocumentId,
                        current.SequenceNumber,
                        current.PrimaryTerm),
                    body,
                    ct);
                if (response.IsSuccessStatusCode) return;
                if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound) continue;
                throw await CreateStoreExceptionAsync("release search write lease", response, ct);
            }
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            _log.LogWarning(
                ex,
                "Failed to release search write lease {OwnerId}/{FencingToken}; it will expire",
                lease.OwnerId,
                lease.FencingToken);
        }
    }

    /// <inheritdoc />
    public async Task<DateTime> GetWatermarkAsync(CancellationToken ct = default) {
        SearchSyncState state = await GetStateAsync(ct);
        return state.WatermarkUtc;
    }

    /// <inheritdoc />
    public Task<bool> SetStateAsync(
        DateTime watermarkUtc,
        string schemaVersion,
        string? expectedConfigurationSignature,
        string configurationSignature,
        CancellationToken ct = default) {
        return WriteStateAsync(
            watermarkUtc,
            schemaVersion,
            null,
            expectedConfigurationSignature,
            configurationSignature,
            ct);
    }

    /// <inheritdoc />
    public Task<bool> SetFullRebuildStateAsync(
        DateTime watermarkUtc,
        string schemaVersion,
        DateTime fullRebuildUtc,
        string? expectedConfigurationSignature,
        string configurationSignature,
        CancellationToken ct = default) {
        return WriteStateAsync(
            watermarkUtc,
            schemaVersion,
            fullRebuildUtc,
            expectedConfigurationSignature,
            configurationSignature,
            ct);
    }

    /// <inheritdoc />
    public async Task<SearchRebuildLease?> TryAcquireRebuildLeaseAsync(
        TimeSpan leaseDuration,
        CancellationToken ct = default) {
        ValidateLeaseDuration(leaseDuration);

        SearchRebuildLease lease = new(
            $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}");

        for (int attempt = 0; attempt < MaxConcurrencyAttempts; attempt++) {
            DateTime expiresAtUtc = DateTime.UtcNow.Add(leaseDuration);
            var body = CreateLeaseBody(
                lease.OwnerId,
                expiresAtUtc,
                null,
                SearchSyncStorage.LeasePhaseBuild);

            using HttpResponseMessage createResponse = await _http.PutAsJsonAsync(
                $"{_stateIndex}/_doc/{SearchSyncStorage.RebuildLeaseDocumentId}?op_type=create&refresh=wait_for",
                body,
                ct);

            if (createResponse.IsSuccessStatusCode) return lease;
            if (createResponse.StatusCode != HttpStatusCode.Conflict) {
                throw await CreateStoreExceptionAsync("acquire rebuild lease", createResponse, ct);
            }

            StoredLease? current = await ReadLeaseAsync(ct);
            if (current == null) continue;
            if (current.LeaseExpiresAtUtc > DateTime.UtcNow) return null;

            using HttpResponseMessage replaceResponse = await _http.PutAsJsonAsync(
                BuildConditionalDocumentUri(
                    SearchSyncStorage.RebuildLeaseDocumentId,
                    current.SequenceNumber,
                    current.PrimaryTerm),
                body,
                ct);

            if (replaceResponse.IsSuccessStatusCode) return lease;
            if (replaceResponse.StatusCode != HttpStatusCode.Conflict
                && replaceResponse.StatusCode != HttpStatusCode.NotFound) {
                throw await CreateStoreExceptionAsync("replace expired rebuild lease", replaceResponse, ct);
            }
        }

        _log.LogWarning("Could not acquire Elasticsearch rebuild lease after {Attempts} ownership races", MaxConcurrencyAttempts);
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> RenewRebuildLeaseAsync(
        SearchRebuildLease lease,
        string? ownedIndex,
        TimeSpan leaseDuration,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        ValidateLeaseDuration(leaseDuration);

        for (int attempt = 0; attempt < MaxConcurrencyAttempts; attempt++) {
            StoredLease? current = await ReadLeaseAsync(ct);
            if (current == null
                || !string.Equals(current.OwnerId, lease.OwnerId, StringComparison.Ordinal)
                || current.LeaseExpiresAtUtc <= DateTime.UtcNow) {
                return false;
            }

            string? nextOwnedIndex = ownedIndex ?? current.OwnedIndex;
            if (string.Equals(current.Phase, SearchSyncStorage.LeasePhaseCutover, StringComparison.Ordinal)) {
                return false;
            }

            var body = CreateLeaseBody(
                lease.OwnerId,
                DateTime.UtcNow.Add(leaseDuration),
                nextOwnedIndex,
                SearchSyncStorage.LeasePhaseBuild);
            using HttpResponseMessage response = await _http.PutAsJsonAsync(
                BuildConditionalDocumentUri(
                    SearchSyncStorage.RebuildLeaseDocumentId,
                    current.SequenceNumber,
                    current.PrimaryTerm),
                body,
                ct);

            if (response.IsSuccessStatusCode) return true;
            if (response.StatusCode != HttpStatusCode.Conflict
                && response.StatusCode != HttpStatusCode.NotFound) {
                throw await CreateStoreExceptionAsync("renew rebuild lease", response, ct);
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> BeginAliasCutoverAsync(
        SearchRebuildLease lease,
        string ownedIndex,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownedIndex);

        for (int attempt = 0; attempt < MaxConcurrencyAttempts; attempt++) {
            StoredLease? current = await ReadLeaseAsync(ct);
            if (current == null
                || !string.Equals(current.OwnerId, lease.OwnerId, StringComparison.Ordinal)
                || current.LeaseExpiresAtUtc <= DateTime.UtcNow
                || !string.Equals(current.OwnedIndex, ownedIndex, StringComparison.Ordinal)
                || !string.Equals(current.Phase, SearchSyncStorage.LeasePhaseBuild, StringComparison.Ordinal)) {
                return false;
            }

            var body = CreateLeaseBody(
                lease.OwnerId,
                DateTime.MaxValue,
                ownedIndex,
                SearchSyncStorage.LeasePhaseCutover);
            using HttpResponseMessage response = await _http.PutAsJsonAsync(
                BuildConditionalDocumentUri(
                    SearchSyncStorage.RebuildLeaseDocumentId,
                    current.SequenceNumber,
                    current.PrimaryTerm),
                body,
                ct);

            if (response.IsSuccessStatusCode) return true;
            if (response.StatusCode != HttpStatusCode.Conflict
                && response.StatusCode != HttpStatusCode.NotFound) {
                throw await CreateStoreExceptionAsync("begin alias cutover", response, ct);
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async Task ReleaseRebuildLeaseAsync(SearchRebuildLease lease, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(lease);

        try {
            for (int attempt = 0; attempt < MaxConcurrencyAttempts; attempt++) {
                StoredLease? current = await ReadLeaseAsync(ct);
                if (current == null
                    || !string.Equals(current.OwnerId, lease.OwnerId, StringComparison.Ordinal)) {
                    return;
                }

                using HttpResponseMessage response = await _http.DeleteAsync(
                    BuildConditionalDocumentUri(
                        SearchSyncStorage.RebuildLeaseDocumentId,
                        current.SequenceNumber,
                        current.PrimaryTerm),
                    ct);

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound) return;
                if (response.StatusCode != HttpStatusCode.Conflict) {
                    throw await CreateStoreExceptionAsync("release rebuild lease", response, ct);
                }
            }
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            // A failed release is safe: the document expires and can be replaced by a later owner.
            _log.LogWarning(ex, "Failed to release rebuild lease owned by {OwnerId}; it will expire", lease.OwnerId);
        }
    }

    private async Task<bool> WriteStateAsync(
        DateTime watermarkUtc,
        string schemaVersion,
        DateTime? fullRebuildUtc,
        string? expectedConfigurationSignature,
        string configurationSignature,
        CancellationToken ct) {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSignature);

        DateTime requestedWatermark = watermarkUtc.ToUniversalTime();
        DateTime? requestedFullRebuild = fullRebuildUtc?.ToUniversalTime();

        for (int attempt = 0; attempt < MaxConcurrencyAttempts; attempt++) {
            StoredState? current;
            try {
                current = await ReadStateAsync(ct);
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                _log.LogWarning(ex, "Failed to read sync state before persisting a checkpoint");
                return false;
            }

            bool isFullRebuild = requestedFullRebuild.HasValue;
            string? currentConfigurationSignature = current?.State.RetailConfigurationSignature;
            bool configurationCanAdvance = string.Equals(
                                                currentConfigurationSignature,
                                                expectedConfigurationSignature,
                                                StringComparison.Ordinal)
                                            || string.Equals(
                                                currentConfigurationSignature,
                                                configurationSignature,
                                                StringComparison.Ordinal);
            if (!configurationCanAdvance) {
                _log.LogWarning(
                    "Refusing stale sync checkpoint: expected configuration {ExpectedSignature}, current is {CurrentSignature}",
                    expectedConfigurationSignature ?? "<unset>",
                    currentConfigurationSignature ?? "<unset>");
                return false;
            }
            bool incrementalPredatesCutover = !isFullRebuild
                                               && current?.State.LastFullRebuildUtc >= requestedWatermark;
            DateTime nextWatermark = isFullRebuild
                ? requestedWatermark
                : incrementalPredatesCutover
                    ? current!.State.WatermarkUtc
                    : current == null || requestedWatermark > current.State.WatermarkUtc
                        ? requestedWatermark
                        : current.State.WatermarkUtc;
            string nextSchemaVersion = incrementalPredatesCutover
                ? current!.State.SchemaVersion ?? schemaVersion
                : schemaVersion;
            DateTime? nextFullRebuild = MaxUtc(current?.State.LastFullRebuildUtc, requestedFullRebuild);
            DateTime? nextFullRebuildStarted = isFullRebuild
                ? requestedWatermark
                : current?.State.LastFullRebuildStartedUtc;
            DateTime? nextIncrementalCatchUp = isFullRebuild
                ? null
                : incrementalPredatesCutover
                    ? current!.State.LastIncrementalCatchUpUtc
                    : MaxUtc(current?.State.LastIncrementalCatchUpUtc, requestedWatermark);

            var body = new {
                lastSyncTime = nextWatermark,
                schemaVersion = nextSchemaVersion,
                lastFullRebuildTime = nextFullRebuild,
                lastFullRebuildStartedTime = nextFullRebuildStarted,
                lastIncrementalCatchUpTime = nextIncrementalCatchUp,
                retailConfigurationSignature = configurationSignature
            };

            string uri = current == null
                ? $"{_stateIndex}/_doc/{SearchSyncStorage.StateDocumentId}?op_type=create&refresh=wait_for"
                : BuildConditionalDocumentUri(
                    SearchSyncStorage.StateDocumentId,
                    current.SequenceNumber,
                    current.PrimaryTerm);

            using HttpResponseMessage response = await _http.PutAsJsonAsync(uri, body, ct);
            if (response.IsSuccessStatusCode) return true;
            if (response.StatusCode == HttpStatusCode.Conflict
                || response.StatusCode == HttpStatusCode.NotFound) {
                continue;
            }

            string error = await response.Content.ReadAsStringAsync(ct);
            _log.LogWarning("Failed to persist sync state: {StatusCode} {Error}", response.StatusCode, error);
            return false;
        }

        _log.LogWarning("Failed to persist sync state after {Attempts} ownership races", MaxConcurrencyAttempts);
        return false;
    }

    private async Task<StoredState?> ReadStateAsync(CancellationToken ct) {
        using HttpResponseMessage response = await _http.GetAsync(
            $"{_stateIndex}/_doc/{SearchSyncStorage.StateDocumentId}?seq_no_primary_term=true",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) {
            throw await CreateStoreExceptionAsync("read sync state", response, ct);
        }

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("_source", out JsonElement source)) {
            throw new InvalidOperationException("Elasticsearch sync state document has no _source.");
        }

        DateTime watermark = ReadUtcDateTime(source, "lastSyncTime") ?? DateTime.MinValue;
        string? schemaVersion = source.TryGetProperty("schemaVersion", out JsonElement version)
            && version.ValueKind == JsonValueKind.String
            ? version.GetString()
            : null;
        DateTime? lastFullRebuild = ReadUtcDateTime(source, "lastFullRebuildTime");
        DateTime? lastFullRebuildStarted = ReadUtcDateTime(
            source,
            "lastFullRebuildStartedTime");
        DateTime? lastIncrementalCatchUp = ReadUtcDateTime(
            source,
            "lastIncrementalCatchUpTime");
        string? retailConfigurationSignature = source.TryGetProperty(
                "retailConfigurationSignature",
                out JsonElement configurationSignature)
            && configurationSignature.ValueKind == JsonValueKind.String
                ? configurationSignature.GetString()
                : null;
        long retailConfigurationEpoch = ReadOptionalInt64(source, "retailConfigurationEpoch");

        return new StoredState(
            new SearchSyncState(
                watermark,
                schemaVersion,
                lastFullRebuild,
                retailConfigurationSignature,
                retailConfigurationEpoch,
                IndexedPricingRevisions: null,
                lastFullRebuildStarted,
                lastIncrementalCatchUp),
            ReadRequiredInt64(root, "_seq_no"),
            ReadRequiredInt64(root, "_primary_term"));
    }

    private async Task<StoredLease?> ReadLeaseAsync(CancellationToken ct) {
        using HttpResponseMessage response = await _http.GetAsync(
            $"{_stateIndex}/_doc/{SearchSyncStorage.RebuildLeaseDocumentId}?seq_no_primary_term=true",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) {
            throw await CreateStoreExceptionAsync("read rebuild lease", response, ct);
        }

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("_source", out JsonElement source)) {
            throw new InvalidOperationException("Elasticsearch rebuild lease document has no _source.");
        }

        string? ownerId = source.TryGetProperty(SearchSyncStorage.LeaseOwnerProperty, out JsonElement owner)
            && owner.ValueKind == JsonValueKind.String
            ? owner.GetString()
            : null;
        string? ownedIndex = source.TryGetProperty(SearchSyncStorage.LeaseIndexProperty, out JsonElement index)
            && index.ValueKind == JsonValueKind.String
            ? index.GetString()
            : null;
        string phase = source.TryGetProperty(SearchSyncStorage.LeasePhaseProperty, out JsonElement phaseElement)
                       && phaseElement.ValueKind == JsonValueKind.String
            ? phaseElement.GetString() ?? SearchSyncStorage.LeasePhaseBuild
            : SearchSyncStorage.LeasePhaseBuild;

        return new StoredLease(
            ownerId,
            ReadUtcDateTime(source, SearchSyncStorage.LeaseExpirationProperty) ?? DateTime.MinValue,
            ownedIndex,
            phase,
            ReadRequiredInt64(root, "_seq_no"),
            ReadRequiredInt64(root, "_primary_term"));
    }

    private async Task<StoredGenerationControl?> ReadGenerationControlAsync(
        CancellationToken ct) {
        using HttpResponseMessage response = await _http.GetAsync(
            $"{_stateIndex}/_doc/{SearchSyncStorage.GenerationControlDocumentId}?seq_no_primary_term=true",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) {
            throw await CreateStoreExceptionAsync("read search generation control", response, ct);
        }

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("_source", out JsonElement source)) {
            throw new InvalidOperationException(
                "Elasticsearch search generation control document has no _source.");
        }

        SearchSyncState state = new(
            ReadUtcDateTime(source, "lastSyncTime") ?? DateTime.MinValue,
            ReadOptionalString(source, "schemaVersion"),
            ReadUtcDateTime(source, "lastFullRebuildTime"),
            ReadOptionalString(source, "retailConfigurationSignature"),
            ReadOptionalInt64(source, "retailConfigurationEpoch"),
            ReadPricingDependencyRevisions(source),
            ReadUtcDateTime(source, "lastFullRebuildStartedTime"),
            ReadUtcDateTime(source, "lastIncrementalCatchUpTime"));

        string? observedConfigurationSignature =
            ReadOptionalString(source, "observedConfigurationSignature")
            ?? state.RetailConfigurationSignature;
        long configurationEpoch = ReadOptionalInt64(source, "configurationEpoch");
        if (configurationEpoch <= 0) configurationEpoch = state.RetailConfigurationEpoch;

        return new StoredGenerationControl(
            state,
            ReadOptionalString(source, "activeIndex"),
            ReadOptionalInt64(source, "activeGeneration"),
            ReadOptionalString(source, "leaseOwnerId"),
            ReadOptionalInt64(source, "leaseFencingToken"),
            ReadUtcDateTime(source, "leaseExpiresAtUtc") ?? DateTime.MinValue,
            ReadOptionalString(source, "stagingIndex"),
            observedConfigurationSignature,
            configurationEpoch,
            ReadOptionalString(source, "leaseConfigurationSignature"),
            ReadOptionalInt64(source, "leaseConfigurationEpoch"),
            ReadRequiredInt64(root, "_seq_no"),
            ReadRequiredInt64(root, "_primary_term"));
    }

    private static bool OwnsCurrentLease(
        StoredGenerationControl? current,
        SearchRebuildLease lease) {
        return current != null
               && current.LeaseExpiresAtUtc > DateTime.UtcNow
               && string.Equals(current.LeaseOwnerId, lease.OwnerId, StringComparison.Ordinal)
               && current.FencingToken == lease.FencingToken;
    }

    private static bool MatchesExpectedGeneration(
        StoredGenerationControl current,
        SearchRebuildLease lease) {
        return current.Generation == lease.ExpectedGeneration
               && string.Equals(
                   current.ActiveIndex,
                   lease.ExpectedActiveIndex,
                   StringComparison.Ordinal);
    }

    private static bool MatchesLeaseGeneration(
        StoredGenerationControl current,
        SearchRebuildLease lease) {
        if (MatchesExpectedGeneration(current, lease)) return true;

        return current.Generation == lease.ExpectedGeneration + 1
               && string.Equals(current.ActiveIndex, current.StagingIndex, StringComparison.Ordinal)
               && current.State.RetailConfigurationEpoch == lease.ConfigurationEpoch
               && string.Equals(
                   current.State.RetailConfigurationSignature,
                   lease.ConfigurationSignature,
                   StringComparison.Ordinal);
    }

    private static bool MatchesConfigurationFence(
        StoredGenerationControl current,
        SearchRebuildLease lease) {
        return lease.HasConfigurationFence
               && current.ConfigurationEpoch == lease.ConfigurationEpoch
               && current.LeaseConfigurationEpoch == lease.ConfigurationEpoch
               && string.Equals(
                   current.ObservedConfigurationSignature,
                   lease.ConfigurationSignature,
                   StringComparison.Ordinal)
               && string.Equals(
                   current.LeaseConfigurationSignature,
                   lease.ConfigurationSignature,
                   StringComparison.Ordinal);
    }

    private static object CreateGenerationControlBody(
        SearchSyncState state,
        string? activeIndex,
        long activeGeneration,
        string? leaseOwnerId,
        long leaseFencingToken,
        DateTime? leaseExpiresAtUtc,
        string? stagingIndex,
        string? observedConfigurationSignature = null,
        long configurationEpoch = 0,
        string? leaseConfigurationSignature = null,
        long leaseConfigurationEpoch = 0) {
        return new {
            activeIndex,
            activeGeneration,
            lastSyncTime = state.WatermarkUtc == DateTime.MinValue
                ? (DateTime?)null
                : state.WatermarkUtc.ToUniversalTime(),
            schemaVersion = state.SchemaVersion,
            lastFullRebuildTime = state.LastFullRebuildUtc?.ToUniversalTime(),
            lastFullRebuildStartedTime = state.LastFullRebuildStartedUtc?.ToUniversalTime(),
            lastIncrementalCatchUpTime = state.LastIncrementalCatchUpUtc?.ToUniversalTime(),
            retailConfigurationSignature = state.RetailConfigurationSignature,
            retailConfigurationEpoch = state.RetailConfigurationEpoch,
            indexedProductPricingRevision = state.IndexedPricingRevisions?.ProductPricing,
            indexedPricingHierarchyRevision = state.IndexedPricingRevisions?.PricingHierarchy,
            indexedDiscountRevision = state.IndexedPricingRevisions?.Discounts,
            indexedExchangeRateRevision = state.IndexedPricingRevisions?.ExchangeRates,
            observedConfigurationSignature,
            configurationEpoch,
            leaseOwnerId,
            leaseFencingToken,
            leaseExpiresAtUtc = leaseExpiresAtUtc?.ToUniversalTime(),
            stagingIndex,
            leaseConfigurationSignature,
            leaseConfigurationEpoch
        };
    }

    private string BuildConditionalDocumentUri(string documentId, long sequenceNumber, long primaryTerm) {
        return $"{_stateIndex}/_doc/{documentId}?if_seq_no={sequenceNumber}&if_primary_term={primaryTerm}&refresh=wait_for";
    }

    private static object CreateLeaseBody(
        string ownerId,
        DateTime leaseExpiresAtUtc,
        string? ownedIndex,
        string phase) {
        return new {
            ownerId,
            leaseExpiresAtUtc = leaseExpiresAtUtc.ToUniversalTime(),
            ownedIndex,
            phase
        };
    }

    private static DateTime? ReadUtcDateTime(JsonElement source, string propertyName) {
        if (!source.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String
            || !value.TryGetDateTime(out DateTime parsed)) {
            throw new InvalidOperationException(
                $"Elasticsearch document property {propertyName} is not a valid UTC date-time.");
        }

        return parsed.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : parsed.ToUniversalTime();
    }

    private static long ReadRequiredInt64(JsonElement root, string propertyName) {
        if (!root.TryGetProperty(propertyName, out JsonElement value)
            || !value.TryGetInt64(out long result)) {
            throw new InvalidOperationException($"Elasticsearch document has no valid {propertyName} value.");
        }

        return result;
    }

    private static long ReadOptionalInt64(JsonElement source, string propertyName) {
        return source.TryGetProperty(propertyName, out JsonElement value)
               && value.TryGetInt64(out long result)
            ? result
            : 0;
    }

    private static string? ReadOptionalString(JsonElement source, string propertyName) {
        return source.TryGetProperty(propertyName, out JsonElement value)
               && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static PricingDependencyRevisions? ReadPricingDependencyRevisions(JsonElement source) {
        PricingDependencyRevisions revisions = new(
            ReadOptionalString(source, "indexedProductPricingRevision") ?? string.Empty,
            ReadOptionalString(source, "indexedPricingHierarchyRevision") ?? string.Empty,
            ReadOptionalString(source, "indexedDiscountRevision") ?? string.Empty,
            ReadOptionalString(source, "indexedExchangeRateRevision") ?? string.Empty);
        return revisions.IsValid ? revisions : null;
    }

    private static DateTime? MaxUtc(DateTime? first, DateTime? second) {
        if (!first.HasValue) return second;
        if (!second.HasValue) return first;
        return first.Value >= second.Value ? first : second;
    }

    private static void ValidateLeaseDuration(TimeSpan leaseDuration) {
        if (leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromMinutes(15)) {
            throw new ArgumentOutOfRangeException(
                nameof(leaseDuration),
                "Lease duration must be positive and no greater than 15 minutes.");
        }
    }

    private static void ValidateConfigurationSignature(string configurationSignature) {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSignature);
        if (configurationSignature.Length > 256) {
            throw new ArgumentOutOfRangeException(
                nameof(configurationSignature),
                "Configuration signature cannot exceed 256 characters.");
        }
    }

    private static async Task<InvalidOperationException> CreateStoreExceptionAsync(
        string operation,
        HttpResponseMessage response,
        CancellationToken ct) {
        string error = await response.Content.ReadAsStringAsync(ct);
        return new InvalidOperationException(
            $"Failed to {operation}: Elasticsearch returned {(int)response.StatusCode} ({response.StatusCode}). {error}");
    }

    private sealed record StoredState(SearchSyncState State, long SequenceNumber, long PrimaryTerm);

    private sealed record StoredLease(
        string? OwnerId,
        DateTime LeaseExpiresAtUtc,
        string? OwnedIndex,
        string Phase,
        long SequenceNumber,
        long PrimaryTerm);

    private sealed record StoredGenerationControl(
        SearchSyncState State,
        string? ActiveIndex,
        long Generation,
        string? LeaseOwnerId,
        long FencingToken,
        DateTime LeaseExpiresAtUtc,
        string? StagingIndex,
        string? ObservedConfigurationSignature,
        long ConfigurationEpoch,
        string? LeaseConfigurationSignature,
        long LeaseConfigurationEpoch,
        long SequenceNumber,
        long PrimaryTerm);
}
