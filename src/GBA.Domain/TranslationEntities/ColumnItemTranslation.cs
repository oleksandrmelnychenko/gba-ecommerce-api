using GBA.Domain.FilterEntities;

namespace GBA.Domain.TranslationEntities;

public class ColumnItemTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long ColumnItemId { get; set; }

    public virtual ColumnItem ColumnItem { get; set; }
}