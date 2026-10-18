using System;
using System.Threading;

namespace GBA.Search.Sync;

public static class SearchSyncActivity {
    private static long _runningSinceTicks;

    public static DateTime? RunningSinceUtc {
        get {
            long ticks = Interlocked.Read(ref _runningSinceTicks);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }
    }

    internal static void MarkStarted(DateTime startedUtc) =>
        Interlocked.Exchange(ref _runningSinceTicks, startedUtc.Ticks);

    internal static void MarkFinished() =>
        Interlocked.Exchange(ref _runningSinceTicks, 0);
}
