using GBA.Domain.FilterEntities;

namespace GBA.Domain.TranslationEntities;

public class FilterItemTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public string Description { get; set; }

    public long FilterItemId { get; set; }

    public virtual FilterItem FilterItem { get; set; }
}