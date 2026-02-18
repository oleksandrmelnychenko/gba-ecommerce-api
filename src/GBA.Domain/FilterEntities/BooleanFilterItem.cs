namespace GBA.Domain.FilterEntities;

public sealed class BooleanFilterItem {
    public string Name { get; set; }

    public string CssClass { get; set; }

    public bool Value { get; set; }

    public string SQL { get; set; }
}