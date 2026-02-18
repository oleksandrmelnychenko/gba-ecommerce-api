using System.Collections.Generic;

namespace GBA.Domain.FilterEntities;

public sealed class GetQuery {
    public GetQuery() {
        SortDescriptors = new List<SortDescriptor>();
    }

    public string Table { get; set; }

    public long Offset { get; set; }

    public long Limit { get; set; }

    public string Filter { get; set; }

    public string BooleanFilter { get; set; }

    public string TypeRoleFilter { get; set; }

    public bool? forReSale { get; set; }

    public IEnumerable<SortDescriptor> SortDescriptors { get; set; }
}