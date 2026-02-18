namespace GBA.Search.Configuration;

public sealed class TypesenseSettings {
    public const string SectionName = "Typesense";
    public string Url { get; set; } = "http://localhost:8108";
    public string ApiKey { get; set; } = string.Empty;
    public string CollectionName { get; set; } = "products";
    public int TimeoutSeconds { get; set; } = 10;
}

public sealed class SyncSettings {
    public const string SectionName = "SearchSync";
    public int IncrementalIntervalSeconds { get; set; } = 60;
    public int FullRebuildHour { get; set; } = 3;
    public int BatchSize { get; set; } = 1000;
    public bool Enabled { get; set; } = true;
    public bool UseAliasSwap { get; set; } = false;
    public bool CleanupOldCollections { get; set; } = true;
    public int CollectionsToKeep { get; set; } = 2;
}

public sealed class ResilienceSettings {
    public const string SectionName = "SearchResilience";
    public int FailureThreshold { get; set; } = 3;
    public int CircuitBreakDurationSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 5;
    public bool EnableFallback { get; set; } = true;
}
