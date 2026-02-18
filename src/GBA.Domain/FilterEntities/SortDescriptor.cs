using GBA.Domain.Entities;

namespace GBA.Domain.FilterEntities;

public sealed class SortDescriptor : EntityBase {
    public string Dir { get; set; }

    public string Column { get; set; }
}