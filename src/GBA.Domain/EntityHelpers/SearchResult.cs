namespace GBA.Domain.EntityHelpers;

public sealed class SearchResult {
    public long Id { get; set; }

    public long RowNumber { get; set; }

    public bool HunderdPrecentMatch { get; set; }

    public bool Available { get; set; }
}