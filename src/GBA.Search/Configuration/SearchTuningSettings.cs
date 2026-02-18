namespace GBA.Search.Configuration;

public sealed class SearchTuningSettings {
    public const string SectionName = "SearchTuning";
    public bool EnableStemming { get; set; } = true;
    public int MaxMergeLimit { get; set; } = 500;
    public string TokenOrder { get; set; } = "unordered";
    public int NumTypos { get; set; } = 2;
    public int TypoTokensThreshold { get; set; } = 2;
    public int MinLen1Typo { get; set; } = 4;
    public int MinLen2Typo { get; set; } = 7;
    public int DropTokensThreshold { get; set; } = 1;
}

public sealed class SearchSynonymsSettings {
    public const string SectionName = "SearchSynonyms";
    public bool Enabled { get; set; } = true;
    public string FilePath { get; set; } = "search_synonyms.txt";
    public int ReloadIntervalSeconds { get; set; } = 60;
}
