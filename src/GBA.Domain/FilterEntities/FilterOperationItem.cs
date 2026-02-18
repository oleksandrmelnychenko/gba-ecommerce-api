using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.FilterEntities;

public class FilterOperationItem : EntityBase {
    public FilterOperationItem() {
        FilterOperatorItemTranslations = new HashSet<FilterOperationItemTranslation>();
    }

    public string Name { get; set; }

    public string SQL { get; set; }

    public virtual ICollection<FilterOperationItemTranslation> FilterOperatorItemTranslations { get; set; }
}