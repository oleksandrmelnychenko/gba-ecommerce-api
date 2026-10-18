using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.ProductModels;

public sealed class SeoProductIndexPage {
    public long TotalCount { get; set; }

    public long Offset { get; set; }

    public int Limit { get; set; }

    public IReadOnlyList<SeoProductIndexItem> Items { get; set; }
}
