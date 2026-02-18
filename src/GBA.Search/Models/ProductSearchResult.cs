using System.Collections.Generic;

namespace GBA.Search.Models;

public sealed class ProductSearchResult {
    public List<long> ProductIds { get; set; } = [];
    public int TotalCount { get; set; }
    public int SearchTimeMs { get; set; }
    public bool IsFallback { get; set; }
    public static ProductSearchResult Empty => new();
}

public sealed class ProductSearchResultWithDocs {
    public List<ProductSearchDocument> Documents { get; set; } = [];
    public int TotalCount { get; set; }
    public int SearchTimeMs { get; set; }
    public bool IsFallback { get; set; }
    public static ProductSearchResultWithDocs Empty => new();
}
