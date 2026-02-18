using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.FilterEntities;

public class FilterItem : EntityBase {
    public FilterItem() {
        FilterItemTranslations = new HashSet<FilterItemTranslation>();
    }

    public string Name { get; set; }

    public string Description { get; set; }

    public string SQL { get; set; }

    public int Order { get; set; }

    public FilterEntityType Type { get; set; }

    public virtual FilterOperationItem FilterOperationItem { get; set; }

    public virtual ICollection<FilterItemTranslation> FilterItemTranslations { get; set; }
}