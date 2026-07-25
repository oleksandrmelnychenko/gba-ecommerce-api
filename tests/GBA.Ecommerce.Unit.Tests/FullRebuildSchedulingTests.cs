using System;
using System.Reflection;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using GBA.Search.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GBA.Ecommerce.Unit.Tests;

/// <summary>
/// The daily rebuild is scheduled by due time, not by matching the current hour: a replica that
/// was down (or busy) at 03:00 must still run the missed rebuild instead of skipping a full day.
/// </summary>
public sealed class FullRebuildSchedulingTests {
    private const int RebuildHour = 3;

    [Fact]
    public void RebuildIsDue_WhenTheConfiguredHourWasMissedEntirely() {
        DateTime lastRebuild = new(2026, 7, 24, 3, 0, 0, DateTimeKind.Utc);
        DateTime nowUtc = new(2026, 7, 25, 11, 0, 0, DateTimeKind.Utc);

        Assert.True(IsFullRebuildDue(nowUtc, StateWithLastRebuild(lastRebuild)));
    }

    [Fact]
    public void RebuildIsNotDue_BeforeTheNextDueTime() {
        DateTime lastRebuild = new(2026, 7, 25, 3, 0, 0, DateTimeKind.Utc);
        DateTime nowUtc = new(2026, 7, 25, 23, 59, 0, DateTimeKind.Utc);

        Assert.False(IsFullRebuildDue(nowUtc, StateWithLastRebuild(lastRebuild)));
    }

    [Fact]
    public void RebuildIsDue_OnceTheNextDayDueTimePasses() {
        DateTime lastRebuild = new(2026, 7, 25, 3, 0, 0, DateTimeKind.Utc);
        DateTime nowUtc = new(2026, 7, 26, 3, 0, 1, DateTimeKind.Utc);

        Assert.True(IsFullRebuildDue(nowUtc, StateWithLastRebuild(lastRebuild)));
    }

    [Fact]
    public void RebuildIsDue_WhenNoRebuildWasEverRecorded() {
        DateTime nowUtc = new(2026, 7, 25, 11, 0, 0, DateTimeKind.Utc);

        Assert.True(IsFullRebuildDue(nowUtc, SearchSyncState.Empty));
    }

    private static SearchSyncState StateWithLastRebuild(DateTime lastRebuildUtc) {
        return new SearchSyncState(
            lastRebuildUtc,
            SearchIndexSchema.CurrentVersion,
            lastRebuildUtc);
    }

    private static bool IsFullRebuildDue(DateTime nowUtc, SearchSyncState state) {
        ProductSearchSyncBackgroundService service = new(
            scopeFactory: null!,
            Options.Create(new SyncSettings { FullRebuildHour = RebuildHour }),
            NullLogger<ProductSearchSyncBackgroundService>.Instance);

        MethodInfo method = typeof(ProductSearchSyncBackgroundService).GetMethod(
            "IsFullRebuildDue",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("IsFullRebuildDue was not found.");

        return (bool)(method.Invoke(service, [nowUtc, state])
            ?? throw new InvalidOperationException("IsFullRebuildDue returned null."));
    }
}
