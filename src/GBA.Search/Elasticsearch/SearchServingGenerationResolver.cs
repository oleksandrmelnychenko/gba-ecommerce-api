using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Configuration;
using Microsoft.Extensions.Options;

namespace GBA.Search.Elasticsearch;

/// <summary>Describes whether one durable search generation is safe to serve.</summary>
public sealed record SearchServingGenerationResolution(
    SearchActiveGeneration? Generation,
    bool SyncStateReadable,
    bool HasActiveGeneration,
    bool SchemaCurrent,
    bool HasWatermark,
    DateTime? LastSyncUtc,
    double? LagSeconds,
    bool Stale,
    bool IncrementalCatchUpRequired,
    DateTime? LastFullRebuildStartedUtc,
    DateTime? LastIncrementalCatchUpUtc,
    IReadOnlyList<string> Reasons) {
    /// <summary>Returns whether the generation may be read by a product search request.</summary>
    public bool IsAvailable => SyncStateReadable
                               && HasActiveGeneration
                               && SchemaCurrent
                               && HasWatermark
                               && !Stale
                               && !IncrementalCatchUpRequired;
}

/// <summary>Resolves and validates the durable generation used by product search.</summary>
public interface ISearchServingGenerationResolver {
    /// <summary>Returns a fail-closed serving assessment without throwing for unavailable state.</summary>
    Task<SearchServingGenerationResolution> ResolveAsync(CancellationToken ct = default);

    /// <summary>Returns the release-ready generation or throws when search cannot be served.</summary>
    Task<SearchActiveGeneration> GetRequiredGenerationAsync(CancellationToken ct = default);
}

/// <summary>Signals that no durable, current, caught-up search generation can be served.</summary>
public sealed class SearchServingUnavailableException : Exception {
    public const string DefaultMessage =
        "Product search is temporarily unavailable because a current caught-up search generation cannot be verified.";

    public SearchServingUnavailableException(SearchServingGenerationResolution resolution)
        : base(DefaultMessage) {
        Resolution = resolution ?? throw new ArgumentNullException(nameof(resolution));
    }

    /// <summary>Gets the fail-closed assessment that rejected the request.</summary>
    public SearchServingGenerationResolution Resolution { get; }
}

/// <summary>Reads and validates the authoritative generation-control document.</summary>
public sealed class SearchServingGenerationResolver(
    ISearchSyncStateStore syncStateStore,
    IOptions<SyncSettings> syncSettings,
    IOptions<ElasticsearchSettings> elasticsearchSettings,
    TimeProvider timeProvider) : ISearchServingGenerationResolver {
    private readonly ISearchSyncStateStore _syncStateStore =
        syncStateStore ?? throw new ArgumentNullException(nameof(syncStateStore));
    private readonly SyncSettings _syncSettings =
        (syncSettings ?? throw new ArgumentNullException(nameof(syncSettings))).Value;
    private readonly string _baseIndexName =
        (elasticsearchSettings ?? throw new ArgumentNullException(nameof(elasticsearchSettings)))
        .Value.IndexName;
    private readonly TimeProvider _timeProvider =
        timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<SearchServingGenerationResolution> ResolveAsync(
        CancellationToken ct = default) {
        SearchActiveGeneration? generation;
        try {
            generation = await _syncStateStore.GetActiveGenerationAsync(ct);
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            throw;
        } catch (Exception) {
            return Unreadable();
        }

        return Evaluate(
            generation,
            _baseIndexName,
            _syncSettings,
            _timeProvider.GetUtcNow().UtcDateTime);
    }

    public async Task<SearchActiveGeneration> GetRequiredGenerationAsync(
        CancellationToken ct = default) {
        SearchServingGenerationResolution resolution = await ResolveAsync(ct);
        if (!resolution.IsAvailable) {
            throw new SearchServingUnavailableException(resolution);
        }

        return resolution.Generation!;
    }

    internal static SearchServingGenerationResolution Evaluate(
        SearchActiveGeneration? generation,
        string baseIndexName,
        SyncSettings settings,
        DateTime utcNow) {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseIndexName);
        ArgumentNullException.ThrowIfNull(settings);

        SearchSyncState? state = generation?.State;
        bool hasActiveGeneration = generation is {
            Generation: > 0,
            IndexName.Length: > 0,
            State: not null
        } && SearchSyncStorage.IsVersionedIndexName(baseIndexName, generation.IndexName);

        if (state == null) {
            return new SearchServingGenerationResolution(
                generation,
                SyncStateReadable: true,
                hasActiveGeneration,
                SchemaCurrent: false,
                HasWatermark: false,
                LastSyncUtc: null,
                LagSeconds: null,
                Stale: true,
                IncrementalCatchUpRequired: true,
                LastFullRebuildStartedUtc: null,
                LastIncrementalCatchUpUtc: null,
                Reasons: hasActiveGeneration
                    ? ["Active search sync state is missing."]
                    : ["No valid active search generation pointer exists."]);
        }

        List<string> reasons = [];
        if (!hasActiveGeneration) {
            reasons.Add("No valid active search generation pointer exists.");
        }

        bool schemaCurrent = string.Equals(
            state.SchemaVersion,
            SearchIndexSchema.CurrentVersion,
            StringComparison.Ordinal);
        if (!schemaCurrent) {
            reasons.Add("Active search generation schema is not current.");
        }

        bool hasWatermark = state.WatermarkUtc != DateTime.MinValue;
        DateTime? watermarkUtc = hasWatermark
            ? state.WatermarkUtc.ToUniversalTime()
            : null;
        double? lagSeconds = watermarkUtc.HasValue
            ? (utcNow.ToUniversalTime() - watermarkUtc.Value).TotalSeconds
            : null;
        bool lagInvalid = settings.LagWarningSeconds <= 0
                          || !lagSeconds.HasValue
                          || lagSeconds.Value < 0
                          || lagSeconds.Value > settings.LagWarningSeconds;
        bool catchUpRequired = !state.HasCompletedRequiredIncrementalCatchUp;

        if (!hasWatermark) {
            reasons.Add("Search sync watermark is missing.");
        } else if (lagSeconds < 0) {
            reasons.Add("Search sync watermark is in the future.");
        } else if (lagSeconds > settings.LagWarningSeconds) {
            reasons.Add("Search sync watermark exceeds the configured lag limit.");
        }

        if (settings.LagWarningSeconds <= 0) {
            reasons.Add("Search sync lag limit is invalid.");
        }
        if (catchUpRequired) {
            reasons.Add("Mandatory incremental catch-up after the latest full rebuild is incomplete.");
        }

        return new SearchServingGenerationResolution(
            generation,
            SyncStateReadable: true,
            hasActiveGeneration,
            schemaCurrent,
            hasWatermark,
            watermarkUtc,
            lagSeconds.HasValue ? Math.Round(lagSeconds.Value, 3) : null,
            Stale: lagInvalid || catchUpRequired || !schemaCurrent,
            IncrementalCatchUpRequired: catchUpRequired,
            state.LastFullRebuildStartedUtc?.ToUniversalTime(),
            state.LastIncrementalCatchUpUtc?.ToUniversalTime(),
            reasons);
    }

    private static SearchServingGenerationResolution Unreadable() => new(
        Generation: null,
        SyncStateReadable: false,
        HasActiveGeneration: false,
        SchemaCurrent: false,
        HasWatermark: false,
        LastSyncUtc: null,
        LagSeconds: null,
        Stale: true,
        IncrementalCatchUpRequired: true,
        LastFullRebuildStartedUtc: null,
        LastIncrementalCatchUpUtc: null,
        Reasons: ["Active search sync state is unreadable."]);
}
