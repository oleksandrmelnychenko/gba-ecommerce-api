using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.FilterEntities;

public class ColumnItem : EntityBase {
    public ColumnItem() {
        ColumnItemTranslations = new HashSet<ColumnItemTranslation>();
    }

    public string Name { get; set; }

    public string CssClass { get; set; }

    public string SQL { get; set; }

    public int Order { get; set; } = 0;

    public FilterEntityType Type { get; set; }

    public string Template { get; set; }

    public long UserId { get; set; }

    public virtual User User { get; set; }

    public virtual ICollection<ColumnItemTranslation> ColumnItemTranslations { get; set; }
}