using GBA.Domain.Entities;

namespace GBA.Domain.TranslationEntities;

public class MeasureUnitTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public string Description { get; set; }

    public long MeasureUnitId { get; set; }

    public virtual MeasureUnit MeasureUnit { get; set; }
}