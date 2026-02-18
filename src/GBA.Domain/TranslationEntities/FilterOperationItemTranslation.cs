using GBA.Domain.FilterEntities;

namespace GBA.Domain.TranslationEntities;

public class FilterOperationItemTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long FilterOperationItemId { get; set; }

    public virtual FilterOperationItem FilterOperationItem { get; set; }
}