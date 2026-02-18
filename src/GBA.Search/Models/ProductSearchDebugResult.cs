using System.Collections.Generic;

namespace GBA.Search.Models;

public sealed class ProductSearchDebugResult {
    public string OriginalQuery { get; set; } = string.Empty;
    public string? Error { get; set; }
    public string NormalizedQuery { get; set; } = string.Empty;
    public string SynonymAppliedQuery { get; set; } = string.Empty;
    public string StemmedQuery { get; set; } = string.Empty;
    public bool StemmingEnabled { get; set; }
    public string QueryType { get; set; } = string.Empty;
    public string TargetCollection { get; set; } = string.Empty;
    public int RequestedLimit { get; set; }
    public int RequestedOffset { get; set; }
    public int MergeLimit { get; set; }
    public DebugPassResult ExactPass { get; set; } = new();
    public DebugPassResult? StemPass { get; set; }
    public List<long> MergedIds { get; set; } = [];
    public List<long> PagedIds { get; set; } = [];
    public int TotalFoundMerged { get; set; }
    public int TotalSearchTimeMs { get; set; }
}

public sealed class DebugPassResult {
    public string Query { get; set; } = string.Empty;
    public int Found { get; set; }
    public int SearchTimeMs { get; set; }
    public List<long> Ids { get; set; } = [];
    public string QueryBy { get; set; } = string.Empty;
    public string Weights { get; set; } = string.Empty;
    public string TokenOrder { get; set; } = string.Empty;
    public string NumTypos { get; set; } = string.Empty;
    public string DropTokensThreshold { get; set; } = string.Empty;
    public string FilterBy { get; set; } = string.Empty;
    public string SortBy { get; set; } = string.Empty;
}
