namespace GBA.Search.Configuration;

public sealed class SyncSettings {
    public const string SectionName = "SearchSync";

    public int IncrementalIntervalSeconds { get; set; } = 60;
    public int FullRebuildHour { get; set; } = 3;
    public int BatchSize { get; set; } = 1000;
    public bool Enabled { get; set; } = true;
    public bool UseAliasSwap { get; set; }
    public bool CleanupOldCollections { get; set; } = true;
    public int CollectionsToKeep { get; set; } = 2;

    /// <summary>Index is considered stale (logged as a warning, reported by /health) once the
    /// watermark is older than this many seconds.</summary>
    public int LagWarningSeconds { get; set; } = 300;
}
